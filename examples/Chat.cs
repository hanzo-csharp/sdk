using System;
using System.Collections.Generic;
using Hanzo.AI.Api;
using Hanzo.AI.Client;
using Hanzo.AI.Model;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// chat — one completion.
    /// Operation: post_chat_completions — POST /v1/chat/completions, the gateway's
    /// own inference route. Non-streaming on purpose: streaming is SSE, which a
    /// generated client hands back as an opaque body. The reply is
    /// choices[0].message.content.
    /// </summary>
    public static class Chat
    {
        /// <summary>A model the gateway serves. HANZO_MODEL overrides it.</summary>
        private const string DefaultModel = "zen5";

        public static int Run()
        {
            var ai = new AiApi(Gateway.Configuration());
            var model = Environment.GetEnvironmentVariable("HANZO_MODEL");
            if (string.IsNullOrWhiteSpace(model))
            {
                model = DefaultModel;
            }
            try
            {
                var reply = ai.PostChatCompletions(new OpenaiChatCompletionRequest(
                    model: model,
                    messages: new List<OpenaiChatCompletionMessage>
                    {
                        new OpenaiChatCompletionMessage(role: "user", content: "Say hello in one short sentence."),
                    }));
                var content = reply.Choices is { Count: > 0 } choices ? choices[0].Message?.Content ?? "" : "";
                Console.WriteLine($"model    {reply.Model}");
                Console.WriteLine($"reply    {content}");
                return 0;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"chat failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
        }
    }
}
