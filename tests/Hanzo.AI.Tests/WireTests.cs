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
            var stub = new Stub("{\"id\":\"c1\",\"object\":\"chat.completion\",\"model\":\"zen5\","
                + "\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"hello\"}}]}");

            var reply = Api<AiApi>(stub, "hk-test", null).PostChatCompletions(
                new OpenaiChatCompletionRequest(
                    model: "zen5",
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
            Assert.Equal("zen5", reply.Model);
            Assert.Equal("hello", reply.Choices[0].Message.Content);
        }

        [Fact]
        public void ARefusalCarriesItsStatusAndBody()
        {
            var stub = new Stub("{\"code\":\"forbidden\"}", HttpStatusCode.Forbidden);

            var e = Assert.Throws<ApiException>(() => Api<AccountApi>(stub, "hk-bad", null).GetAccountKeys());

            Assert.Equal(403, e.ErrorCode);
            Assert.Equal("{\"code\":\"forbidden\"}", e.ErrorContent?.ToString());
        }
    }
}
