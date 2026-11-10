using System;
using System.Collections.Generic;
using System.Linq;
using Hanzo.AI.Api;
using Hanzo.AI.Client;
using Hanzo.AI.Model;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanzo.AI.Tests
{
    /// <summary>
    /// End-to-end live integration tests against https://api.hanzo.ai when HANZO_API_KEY is present.
    /// No-ops cleanly in offline/unauthenticated unit-test runs (`HANZO_OFFLINE=1` or empty key).
    /// </summary>
    public class LiveIntegrationTests
    {
        private static bool ShouldRunLive(out Configuration config)
        {
            config = Gateway.Configuration();
            if (Environment.GetEnvironmentVariable("HANZO_OFFLINE") == "1")
            {
                return false;
            }
            return !string.IsNullOrWhiteSpace(config.AccessToken);
        }

        [Fact]
        public void LiveCatalogIncludesZen6KaiAndZenVoice()
        {
            if (!ShouldRunLive(out var config)) return;

            var models = new AiApi(config).GetModels();
            var ids = models.Data.Select(m => m.Id).ToHashSet();

            Assert.Contains("zen6", ids);
            Assert.Contains("zen6-flash", ids);
            Assert.Contains("zen6-coder", ids);
            Assert.Contains("zen6-max", ids);
            Assert.Contains("kai", ids);
            Assert.Contains("zen-voice", ids);
        }

        [Fact]
        public void LiveGameBackendLoopKvKaiZen6AndZenVoice()
        {
            if (!ShouldRunLive(out var config)) return;

            var kv = new KvApi(config);
            var ai = new AiApi(config);
            var bucket = "cs_sdk_test_" + Guid.NewGuid().ToString("N").Substring(0, 10);

            try
            {
                // 1. Revisioned Cloud KV save-game round-trip
                var created = kv.PostKvByBucket(bucket, new KvBucketWrite(history: 3));
                Assert.Equal(bucket, created.Bucket);

                var rev1 = kv.PutKvByBucketByKey(bucket, "slot_1", new KvKvWrite(value: "{\"zone\":\"outpost\",\"hp\":100}"));
                Assert.Equal(1, rev1.Revision);

                var rev2 = kv.PutKvByBucketByKey(bucket, "slot_1", new KvKvWrite(value: "{\"zone\":\"reactor\",\"hp\":35,\"threat\":true}"));
                Assert.Equal(2, rev2.Revision);

                var entry = kv.GetKvByBucketByKey(bucket, "slot_1");
                Assert.Equal(2, entry.Revision);
                var stateJson = JObject.Parse(entry.Value);
                Assert.Equal("reactor", stateJson["zone"]!.Value<string>());
                Assert.Equal(35, stateJson["hp"]!.Value<int>());

                var history = kv.GetKvByBucketByKeyHistory(bucket, "slot_1");
                Assert.Equal(2, history.Data.Count);

                // 2. Zero-output-token Kai decision from loaded save state
                var decision = ai.PostDecisions(new AiDecisionsRequest(
                    model: "kai",
                    state: new { zone = "reactor", hp = 35, threat = true },
                    questions: new Dictionary<string, AiDecisionsQuestion>
                    {
                        ["action"] = new AiDecisionsQuestion(new AiDecisionsChoice(
                            type: AiDecisionsChoice.TypeEnum.Choice,
                            instructions: "Choose the survival tactic when critically injured in a dangerous zone.",
                            criteria: new Dictionary<string, string>
                            {
                                ["retreat_to_cover"] = "fall back and heal when hp < 45 under threat",
                                ["charge_blindly"] = "rush forward only when at full health",
                            })),
                    }));
                Assert.Equal("retreat_to_cover", decision.Answers["action"].Choice);
                Assert.Equal(0, decision.Usage.OutputTokens);

                // 3. Zen 6 dialogue completion (`zen6-flash` and `zen6`)
                var flash = ai.PostChatCompletions(new OpenaiChatCompletionRequest(
                    model: "zen6-flash",
                    maxTokens: 200,
                    temperature: 0.1m,
                    messages: new List<OpenaiChatCompletionMessage>
                    {
                        new OpenaiChatCompletionMessage(role: "user", content: "Reply with exact token: CSHARP_FLASH_OK"),
                    }));
                Assert.Contains("CSHARP_FLASH_OK", flash.Choices[0].Message.Content);

                var zen6 = ai.PostChatCompletions(new OpenaiChatCompletionRequest(
                    model: "zen6",
                    maxTokens: 1000,
                    temperature: 0.2m,
                    messages: new List<OpenaiChatCompletionMessage>
                    {
                        new OpenaiChatCompletionMessage(role: "user", content: "Say hello in three words."),
                    }));
                Assert.False(string.IsNullOrWhiteSpace(zen6.Choices[0].Message.Content));

                // 4. Zen Voice MP3 synthesis (`zen-voice`)
                var mp3Bytes = ai.PostAudioSpeechBytes("Supply drop inbound to reactor.", "alloy", "zen-voice");
                Assert.True(mp3Bytes.Length > 1000);
            }
            finally
            {
                try { kv.DeleteKvByBucket(bucket); } catch { /* ignore cleanup error */ }
            }
        }
    }
}
