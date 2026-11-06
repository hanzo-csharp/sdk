// The six canonical flows. They are data in hanzoai/openapi flows.yaml — a closed
// set every Hanzo SDK owes — so adding a seventh here is not this repo's choice.
//
//   HANZO_API_KEY=hk-... dotnet run --project examples -- hello
using System;
using Hanzo.AI.Examples;

var flows = new (string Name, Func<int> Run)[]
{
    ("hello", Hello.Run),
    ("chat", Chat.Run),
    ("money", Money.Run),
    ("store", Store.Run),
    ("agent", Agent.Run),
    ("tools", Tools.Run),
};

var want = args.Length > 0 ? args[0] : "";
foreach (var flow in flows)
{
    if (flow.Name == want)
    {
        return flow.Run();
    }
}
Console.Error.WriteLine($"usage: dotnet run -- <{string.Join("|", Array.ConvertAll(flows, f => f.Name))}>");
return 2;
