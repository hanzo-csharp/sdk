using System;
using Hanzo.AI.Api;
using Hanzo.AI.Client;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// money — balance, then the usage that moved it.
    /// Operations: get_billing_balance, get_billing_usage. Neither takes an org: the
    /// tenant comes from the token's owner claim, so a key reads only its own money.
    /// The document publishes no response schema for either, so the generated
    /// methods return nothing and this prints the status. When cloud declares the
    /// shapes, a regeneration gives them bodies.
    /// </summary>
    public static class Money
    {
        public static int Run()
        {
            var billing = new BillingApi(Gateway.Configuration());
            try
            {
                Console.WriteLine($"balance  HTTP {billing.GetBillingBalanceWithHttpInfo().StatusCode:D}");
                Console.WriteLine($"usage    HTTP {billing.GetBillingUsageWithHttpInfo().StatusCode:D}");
                return 0;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"billing failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
        }
    }
}
