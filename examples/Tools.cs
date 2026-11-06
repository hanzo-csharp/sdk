using System;
using Hanzo.AI.Api;
using Hanzo.AI.Client;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// tools — the tool catalog this key can reach.
    /// Operation: get_tool — GET /v1/tool.
    /// </summary>
    public static class Tools
    {
        public static int Run()
        {
            var tools = new ToolApi(Gateway.Configuration());
            try
            {
                var listed = tools.GetTool().Tools;
                if (listed == null || listed.Count == 0)
                {
                    Console.WriteLine("no tools reachable with this key");
                    return 0;
                }
                foreach (var tool in listed)
                {
                    Console.WriteLine($"{tool.Name,-32} {tool.Source,-12} {(tool.Activated == true ? "activated" : "available")}");
                }
                Console.WriteLine();
                Console.WriteLine($"{listed.Count} tools");
                return 0;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"tools failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
        }
    }
}
