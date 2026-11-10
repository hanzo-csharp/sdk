using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hanzo.AI.Api;
using Hanzo.AI.Client;
using Hanzo.AI.Model;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanzo.AI.Tests
{
    /// <summary>
    /// What the client puts on the wire and what it reads back, against a stub
    /// transport: the credential, the tenant, the routes the document marks
    /// public, a response that grows a field, the fields a request sends, and a
    /// refusal. The build proves the client matches the document; these prove a
    /// request built from it is the one the gateway expects.
    /// </summary>
    public class WireTests
    {
        private sealed class Stub : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public HttpRequestMessage? Request;
            public string? Sent;

            public Stub(string body, HttpStatusCode status = HttpStatusCode.OK)
            {
                _body = body;
                _status = status;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Request = request;
                Sent = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
                return new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json"),
                    RequestMessage = request,
                };
            }
        }

        private const string Base = "https://gateway.test";

        private static T Api<T>(Stub stub, string? key, string? org)
        {
            var config = Gateway.Configuration(key, Base, org);
            return (T)System.Activator.CreateInstance(typeof(T), new HttpClient(stub), config, null)!;
        }

        private static string? Header(HttpRequestMessage r, string name) =>
            r.Headers.TryGetValues(name, out var v) ? string.Join(",", v) : null;

        [Fact]
        public void DefaultsToTheGateway()
        {
            Assert.Equal("https://api.hanzo.ai", Gateway.Configuration(null, null, null).BasePath);
            Assert.Equal("https://api.hanzo.ai", Gateway.Configuration("", "", "").BasePath);
        }

        [Fact]
        public void SendsTheBearerAndTheTenant()
        {
            var stub = new Stub("{\"keys\":[{\"id\":\"k1\",\"prefix\":\"hk-ab\",\"created\":\"2026-11-01T00:00:00Z\"}]}");

            var list = Api<AccountApi>(stub, "hk-test", "acme").GetAccountKeys();

            Assert.Equal(HttpMethod.Get, stub.Request!.Method);
            Assert.Equal(Base + "/v1/account/keys", stub.Request.RequestUri!.ToString());
            Assert.Equal("Bearer hk-test", Header(stub.Request, "Authorization"));
            Assert.Equal("acme", Header(stub.Request, "X-Org-Id"));
            var key = list.Keys.Single();
            Assert.Equal("k1", key.Id);
            Assert.Equal("hk-ab", key.Prefix);
            Assert.Equal("2026-11-01T00:00:00Z", key.Created);
        }

        [Fact]
        public void SendsNoCredentialWithoutAKey()
        {
            var stub = new Stub("{\"keys\":[]}");

            Api<AccountApi>(stub, null, null).GetAccountKeys();

            Assert.Null(Header(stub.Request!, "Authorization"));
            Assert.Null(Header(stub.Request!, "X-Org-Id"));
        }

        [Fact]
        public void SendsNoCredentialWhereTheDocumentSaysNone()
        {
            // GET /v1/openapi.json carries `security: []`: the document is public.
            var stub = new Stub("{}");

            Api<OpenapiApi>(stub, "hk-test", null).GetOpenapiJson();

            Assert.Equal(Base + "/v1/openapi.json", stub.Request!.RequestUri!.ToString());
            Assert.Null(Header(stub.Request, "Authorization"));
        }

        [Fact]
        public void ReadsAFieldTheDocumentDoesNotDeclare()
        {
            var stub = new Stub("{\"keys\":[{\"id\":\"k1\",\"grown\":\"later\"}]}");

            var key = Api<AccountApi>(stub, "hk-test", null).GetAccountKeys().Keys.Single();

            Assert.Equal("k1", key.Id);
            Assert.Equal("later", key.AdditionalProperties["grown"]?.ToString());
        }

        [Fact]
        public void SendsTheCompletionAndItsBounds()
        {
            var stub = new Stub("{\"id\":\"c1\",\"object\":\"chat.completion\",\"model\":\"zen6\","
                + "\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"hello\"}}]}");

            var reply = Api<AiApi>(stub, "hk-test", null).PostChatCompletions(
                new OpenaiChatCompletionRequest(
                    model: "zen6",
                    messages: new List<OpenaiChatCompletionMessage> { new OpenaiChatCompletionMessage(role: "user", content: "hi") },
                    temperature: 0),
                xMaxCost: 0.5m, xMaxLatencyMs: 2000);

            Assert.Equal(HttpMethod.Post, stub.Request!.Method);
            Assert.Equal(Base + "/v1/chat/completions", stub.Request.RequestUri!.ToString());
            Assert.Equal("0.5", Header(stub.Request, "X-Max-Cost"));
            Assert.Equal("2000", Header(stub.Request, "X-Max-Latency-Ms"));
            var body = JObject.Parse(stub.Sent!);
            // Exactly what was set: an explicit 0 goes out, and nothing unset does.
            Assert.Equal(new[] { "messages", "model", "temperature" }, body.Properties().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal(0m, body["temperature"]!.Value<decimal>());
            Assert.Equal(new[] { "content", "role" }, ((JObject)body["messages"]![0]!).Properties().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal("zen6", reply.Model);
            Assert.Equal("hello", reply.Choices[0].Message.Content);
        }

        [Fact]
        public void SendsEveryRequiredField()
        {
            // A required constructor argument is on the wire. The generator's
            // conditional serialization used to leave each one unflagged, so
            // `model` and a question's `type` and `criteria` were dropped and the
            // gateway answered 400 "the request needs a 'model'".
            // A reply in the shape /v1/decisions answers with, captured live.
            var stub = new Stub("{\"id\":\"dec_1\",\"model\":\"kai\",\"provider\":\"Hanzo\",\"answers\":{\"move\":{\"type\":\"choice\",\"choice\":\"b\",\"confidence\":0.5,\"probabilities\":{\"a\":0.25,\"b\":0.75}}},\"usage\":{\"input_tokens\":10,\"output_tokens\":0},\"routing\":{\"backend\":\"kai\",\"checkpoint\":\"kai-1.10\",\"sha256\":\"8069bf5cc5e897ce28399d3a5d2a17097e3859d85e5a4cda50c7f19f1e60e9f4\",\"representation\":\"6e386752d1525bf8ea0d90f6a82ec89091b32514ccbc7cb5acf5ac332a95f613\",\"calibration\":\"cal_c6ffe2e7b0c8819a\",\"device\":\"cpu\",\"reach\":32768,\"trained\":1024,\"positions\":8192,\"extrapolated\":false,\"reason\":\"explicit model='kai'\"},\"state_hash\":\"sha256:8770c944ed8823932f7b488043302076da0aec08b9bc4ea8506cf3ea7d0bdbc5\",\"latency_ms\":67.436068}");

            var reply = Api<AiApi>(stub, "hk-test", null).PostDecisions(new AiDecisionsRequest(
                model: "kai",
                state: new { board = "x" },
                questions: new Dictionary<string, AiDecisionsQuestion>
                {
                    ["move"] = new AiDecisionsQuestion(new AiDecisionsChoice(
                        type: AiDecisionsChoice.TypeEnum.Choice,
                        criteria: new Dictionary<string, string> { ["a"] = "first", ["b"] = "second" })),
                }));

            Assert.Equal(Base + "/v1/decisions", stub.Request!.RequestUri!.ToString());
            var body = JObject.Parse(stub.Sent!);
            Assert.Equal("kai", body["model"]!.Value<string>());
            Assert.Equal("x", body["state"]!["board"]!.Value<string>());
            var move = (JObject)body["questions"]!["move"]!;
            Assert.Equal(new[] { "criteria", "type" }, move.Properties().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal("choice", move["type"]!.Value<string>());
            Assert.Equal("second", move["criteria"]!["b"]!.Value<string>());
            Assert.Equal("b", reply.Answers["move"].Choice);
            Assert.Equal(0.75m, reply.Answers["move"].Probabilities["b"]);
        }

        [Fact]
        public void ARefusalCarriesItsStatusAndBody()
        {
            var stub = new Stub("{\"code\":\"forbidden\"}", HttpStatusCode.Forbidden);

            var e = Assert.Throws<ApiException>(() => Api<AccountApi>(stub, "hk-bad", null).GetAccountKeys());

            Assert.Equal(403, e.ErrorCode);
            Assert.Equal("{\"code\":\"forbidden\"}", e.ErrorContent?.ToString());
        }

        [Fact]
        public void SendsAgentRunInputAsJson()
        {
            var stub = new Stub("{\"id\":\"run_123\",\"status\":\"ok\",\"model\":\"zen6-flash\",\"output\":\"Tokyo is the capital of Japan.\"}");

            var run = Api<AgentApi>(stub, "hk-test", "acme").PostAgentByRefRun("bot-1", "What is the capital of Japan?");

            Assert.Equal(HttpMethod.Post, stub.Request!.Method);
            Assert.Equal(Base + "/v1/agent/bot-1/run", stub.Request.RequestUri!.ToString());
            Assert.Equal("Bearer hk-test", Header(stub.Request, "Authorization"));
            Assert.Equal("acme", Header(stub.Request, "X-Org-Id"));
            var body = JObject.Parse(stub.Sent!);
            Assert.Equal("What is the capital of Japan?", body["input"]!.Value<string>());
            Assert.Equal("run_123", run.Id);
            Assert.Equal("ok", run.Status);
            Assert.Equal("Tokyo is the capital of Japan.", run.Output);
        }
    }
}
