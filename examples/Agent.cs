using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hanzo.AI.Api;
using Hanzo.AI.Client;
using Hanzo.AI.Model;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// agent — create an agent, run it, poll until the run settles, print its output.
    /// Operations: post_agent, post_agent_by_ref_run, get_agent_by_ref_runs.
    /// </summary>
    public static class Agent
    {
        /// <summary>Anything not still moving is done; the document does not enumerate the vocabulary.</summary>
        private static readonly HashSet<string> Running = new() { "queued", "pending", "running", "in_progress", "starting" };
        private const int PollAttempts = 60;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
        /// <summary>The only flow that names a model. HANZO_MODEL overrides it.</summary>
        private const string DefaultModel = "zen5";

        public static int Run()
        {
            var agents = new AgentApi(Gateway.Configuration());
            var name = "sdk-example-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var model = Environment.GetEnvironmentVariable("HANZO_MODEL");
            try
            {
                var created = agents.PostAgent(new AgentCreateAgentIn(
                    name: name,
                    model: string.IsNullOrWhiteSpace(model) ? DefaultModel : model,
                    instructions: "Answer in exactly one sentence: what is the capital of Japan?"));
                Console.WriteLine($"created  {created.Name} ({created.Id})");

                var before = Ids(agents.GetAgentByRefRuns(name, 100));
                agents.PostAgentByRefRun(name);
                Console.WriteLine($"ran      {name}");

                for (var attempt = 0; attempt < PollAttempts; attempt++)
                {
                    var run = agents.GetAgentByRefRuns(name, 100).Runs?.FirstOrDefault(r => !before.Contains(r.Id));
                    if (run != null && !Running.Contains(run.Status ?? ""))
                    {
                        Console.WriteLine($"run      {run.Id} {run.Status}");
                        Console.WriteLine($"output   {run.Output}");
                        return 0;
                    }
                    Thread.Sleep(PollInterval);
                }
                Console.Error.WriteLine($"run did not finish within {PollAttempts * PollInterval.TotalSeconds}s");
                return 1;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"agent failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
        }

        private static HashSet<string> Ids(AgentRunList? list) =>
            new(list?.Runs?.Select(r => r.Id) ?? Enumerable.Empty<string>());
    }
}
