using System;
using System.Linq;
using Hanzo.AI.Api;
using Hanzo.AI.Client;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// hello — prove the key works and print what it owns.
    /// Operation: get_account_keys — GET /v1/account/keys. With no key or a bad one
    /// it answers 403; that refusal is the point of the flow.
    /// </summary>
    public static class Hello
    {
        public static int Run()
        {
            var keys = new AccountApi(Gateway.Configuration());
            try
            {
                var owned = keys.GetAccountKeys().Keys;
                if (owned == null || owned.Count == 0)
                {
                    Console.WriteLine("the key is good, and it owns no keys of its own");
                    return 0;
                }
                foreach (var key in owned)
                {
                    Console.WriteLine($"{key.Type,-8} {key.Prefix,-24} {key.Created}");
                }
                return 0;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"keys refused: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
        }
    }
}
