# Hanzo — C# SDK

C# client for the [Hanzo](https://hanzo.ai) API, covering every `/v1` route the
gateway at `api.hanzo.ai` serves. It is generated from the API's own OpenAPI
document, the one published at
[`/v1/openapi.json`](https://api.hanzo.ai/v1/openapi.json), so a method is that
document's operation id Pascal-cased: `get_account_keys` → `GetAccountKeys`,
`post_chat_completions` → `PostChatCompletions`. Every method has an `…Async`
twin and a `…WithHttpInfo` form that also returns the status and headers.

[`.spec-lock`](.spec-lock) names the cloud release ref and the digest of the
bytes this client was cut from.

## Install

Published as `Hanzo` (.NET 8 or newer, including Godot 4 .NET) on
[GitHub Packages](https://github.com/orgs/hanzo-csharp/packages/nuget/package/Hanzo),
[GitHub Releases](https://github.com/hanzo-csharp/sdk/releases), and
[nuget.org](https://www.nuget.org/packages/Hanzo):

```bash
# nuget.org (once NUGET_API_KEY is provisioned) or configured GitHub Packages source:
dotnet add package Hanzo --version 8.5.1027
```

To install directly from GitHub Packages (`https://nuget.pkg.github.com/hanzo-csharp/index.json`):

```bash
dotnet nuget add source "https://nuget.pkg.github.com/hanzo-csharp/index.json" \
  --name hanzo-github --username "$GITHUB_USER" --password "$(gh auth token)" --store-password-in-clear-text
dotnet add package Hanzo --version 8.5.1027
```

Namespaces: `Hanzo.AI` (the entry point), `Hanzo.AI.Api`, `Hanzo.AI.Model`,
`Hanzo.AI.Client`.

## Authenticate

The document declares one security scheme, `bearer`, and applies it to every
operation but the few it marks open. `Configuration.AccessToken` is where the
token goes; each generated call that needs it sends `Authorization: Bearer …`.

[`Gateway`](src/Hanzo.AI/Gateway.cs) builds that configuration from the
environment, and is the only place in the package that reads it:

| variable | meaning |
| --- | --- |
| `HANZO_API_KEY` | bearer credential: a Cloud API key, or a Hanzo IAM access token |
| `HANZO_BASE_URL` | gateway to talk to; default `https://api.hanzo.ai` |
| `HANZO_ORG_ID` | org scope, sent as `X-Org-Id`. Without it the tenant is the token's `owner` claim |

For a program that serves more than one tenant, pass them instead:
`Gateway.Configuration(token, null, "acme")`.

## Quickstart

```csharp
using Hanzo.AI;
using Hanzo.AI.Api;
using Hanzo.AI.Model;

var config = Gateway.Configuration();

foreach (var key in new AccountApi(config).GetAccountKeys().Keys)
{
    Console.WriteLine($"{key.Type} {key.Prefix}");
}

var reply = new AiApi(config).PostChatCompletions(new OpenaiChatCompletionRequest(
    model: "zen6",
    maxTokens: 900,
    messages: new() { new OpenaiChatCompletionMessage(role: "user", content: "Say hello.") }));
Console.WriteLine(reply.Choices[0].Message.Content);
```

`GET /v1/account/keys` refuses without a credential, `403
{"code":"forbidden","detail":"sign in to manage API keys",…}`, so reaching the
loop proves the key works. A refusal arrives as `Hanzo.AI.Client.ApiException`
carrying `ErrorCode` and `ErrorContent`.

A request carries the fields you set and only those: an optional number or
flag is `T?`, an unset one is left off the wire, and an explicit `0` or `false`
is sent.

## Examples

Eight runnable flows, each a complete program — the six canonical SDK flows
from `hanzoai/openapi` `flows.yaml` plus `kai` (`/v1/decisions`) and
`game-backend` (revisioned Cloud KV saves + Kai Director + Zen 6 radio + Zen Voice MP3).
The build compiles them against the client.

| flow | what it does |
|---|---|
| [`hello`](examples/Hello.cs) | `GET /v1/account/keys` — prove the key works |
| [`chat`](examples/Chat.cs) | `POST /v1/chat/completions` (`zen6`) — one completion |
| [`kai`](examples/Kai.cs) | `POST /v1/decisions` (`kai`) — structured decision |
| [`money`](examples/Money.cs) | `GET /v1/billing/balance`, `GET /v1/billing/usage` |
| [`store`](examples/Store.cs) | `POST /v1/provisioning/kv`, then `GET` and `DELETE /v1/provisioning/kv/{name}` |
| [`agent`](examples/Agent.cs) | create a budgeted agent, run it, poll to completion, delete it |
| [`tools`](examples/Tools.cs) | `GET /v1/tool` — the tool catalog this key reaches |
| [`game-backend`](examples/GameBackend.cs) | `/v1/kv` save slots -> `kai` Director -> `zen6-flash` radio -> `zen-voice` MP3 |

```bash
HANZO_API_KEY=… ./scripts/dotnet run --project examples -- hello
```

## How it is made

`src/Hanzo.AI/{Api,Client,Model}` is generated and replaced on every
regeneration; never edit a file there. `Gateway.cs`, the csproj, the tests and
the examples are this repo's.

```bash
SPEC=/path/to/openapi.yaml OPENAPI=/path/to/hanzoai/openapi ./scripts/generate.sh
./scripts/generate.sh --check   # non-zero if the committed client drifted
```

`scripts/generate.sh` passes no flags: the invocation lives once, in
`hanzoai/openapi/generate.py`, and every knob (generator, HTTP library, name
mappings) is the `csharp` row of its `sdks.yaml`. Each hanzo-inc/cloud release
dispatches this repo's CI with the commit it deployed. CI regenerates from that
document, runs the gates in [`hanzo.yml`](hanzo.yml), commits, and cuts the
release's version, and [`release.yml`](.github/workflows/release.yml) publishes
it to nuget.org.

`./scripts/dotnet` is `dotnet`, installing the .NET 10 SDK into `~/.dotnet`
first on a machine that has none.

## The same API in other languages

[Python](https://github.com/hanzoai/python-sdk) ·
[TypeScript](https://github.com/hanzoai/js-sdk) ·
[Go](https://github.com/hanzo-go/sdk) ·
[Rust](https://github.com/hanzo-rs/sdk) ·
[Java](https://github.com/hanzo-java/sdk) ·
[C++](https://github.com/hanzo-cpp/sdk) ·
[Godot](https://github.com/hanzoai/godot-sdk) ·
[all SDKs](https://github.com/hanzoai/sdk)

## License

Apache-2.0. See [LICENSE](LICENSE).
