using System;
using System.Collections.Generic;
using Hanzo.AI.Api;
using Hanzo.AI.Client;
using Hanzo.AI.Model;
using Newtonsoft.Json.Linq;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// game-backend — Complete game backend slice on https://api.hanzo.ai:
    ///   1. Revisioned Cloud KV save slot (`/v1/kv/{bucket}/{key}`)
    ///   2. Zero-output-token Kai AI Director decision (`/v1/decisions` on `kai`)
    ///   3. Grounded Zen 6 radio line (`/v1/chat/completions` on `zen6-flash`)
    ///   4. Zen Voice MP3 synthesis (`/v1/audio/speech` on `zen-voice`)
    /// </summary>
    public static class GameBackend
    {
        public static int Run()
        {
            var config = Gateway.Configuration();
            var kv = new KvApi(config);
            var ai = new AiApi(config);
            var bucket = "cs_game_saves_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            try
            {
                kv.PostKvByBucket(bucket, new KvBucketWrite(history: 4));
                var ack = kv.PutKvByBucketByKey(bucket, "slot_1", new KvKvWrite(
                    value: "{\"player\":\"ranger_01\",\"zone\":\"reactor_breach\",\"hp\":34,\"ammo\":5}"));
                var entry = kv.GetKvByBucketByKey(bucket, "slot_1");
                var save = JObject.Parse(entry.Value);
                Console.WriteLine($"1. cloud_save   bucket={bucket} rev={ack.Revision} zone={save["zone"]} hp={save["hp"]}");

                var dec = ai.PostDecisions(new AiDecisionsRequest(
                    model: "kai",
                    state: new { zone = save["zone"]?.ToString(), hp = save["hp"]?.Value<int>(), ammo = save["ammo"]?.Value<int>() },
                    questions: new Dictionary<string, AiDecisionsQuestion>
                    {
                        ["director"] = new AiDecisionsQuestion(new AiDecisionsChoice(
                            type: AiDecisionsChoice.TypeEnum.Choice,
                            instructions: "Choose the Game Director action for the player's current save state.",
                            criteria: new Dictionary<string, string>
                            {
                                ["dispatch_supply_drop"] = "drop medkit and ammo when player hp < 45",
                                ["escalate_ambush"] = "spawn heavy patrol when player is near full health",
                            })),
                    }));
                var ans = dec.Answers["director"];
                Console.WriteLine($"2. kai_director choice={ans.Choice} confidence={ans.Confidence:F3} output_tokens={dec.Usage.OutputTokens}");

                var chat = ai.PostChatCompletions(new OpenaiChatCompletionRequest(
                    model: "zen6-flash",
                    maxTokens: 250,
                    temperature: 0.3m,
                    messages: new List<OpenaiChatCompletionMessage>
                    {
                        new OpenaiChatCompletionMessage(role: "system", content: "Transmit one terse tactical radio sentence under 18 words."),
                        new OpenaiChatCompletionMessage(role: "user", content: $"Player state: {entry.Value}. Director action: {ans.Choice}."),
                    }));
                var line = chat.Choices[0].Message.Content?.Trim() ?? "Supply cache inbound.";
                Console.WriteLine($"3. zen6_radio   ({chat.Model}): {line}");

                var mp3 = ai.PostAudioSpeechBytes(line, "alloy", "zen-voice");
                Console.WriteLine($"4. zen_voice    bytes={mp3.Length} format=mp3");
                return 0;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"game-backend failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
            finally
            {
                try
                {
                    kv.DeleteKvByBucket(bucket);
                    Console.WriteLine($"5. cleanup      deleted={bucket}");
                }
                catch { /* ignore cleanup error */ }
            }
        }
    }
}
