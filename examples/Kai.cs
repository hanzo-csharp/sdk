using System;
using System.Collections.Generic;
using Hanzo.AI.Api;
using Hanzo.AI.Client;
using Hanzo.AI.Model;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// kai — one structured decision via Kai.
    /// Operation: post_decisions — POST /v1/decisions with model "kai".
    /// </summary>
    public static class Kai
    {
        public static int Run()
        {
            var ai = new AiApi(Gateway.Configuration());
            try
            {
                var reply = ai.PostDecisions(new AiDecisionsRequest(
                    model: "kai",
                    state: new
                    {
                        npc = "guard_01",
                        health = 82,
                        ammo = 14,
                        sound_contact = true,
                        distance_m = 11.5,
                    },
                    questions: new Dictionary<string, AiDecisionsQuestion>
                    {
                        ["tactic"] = new AiDecisionsQuestion(new AiDecisionsChoice(
                            type: AiDecisionsChoice.TypeEnum.Choice,
                            criteria: new Dictionary<string, string>
                            {
                                ["patrol"] = "keep moving along route when area is quiet",
                                ["investigate"] = "move cautiously toward sound contact",
                                ["engage"] = "open fire on confirmed visible target",
                                ["retreat"] = "fall back when critically injured or out of ammo",
                            })),
                    }));
                var answer = reply.Answers["tactic"];
                Console.WriteLine($"model    {reply.Model}");
                Console.WriteLine($"choice   {answer.Choice} (confidence {answer.Confidence:0.###})");
                Console.WriteLine($"latency  {reply.LatencyMs:0.#} ms");
                return 0;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"kai failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
        }
    }
}
