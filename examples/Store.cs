using System;
using Hanzo.AI.Api;
using Hanzo.AI.Client;
using Hanzo.AI.Model;

namespace Hanzo.AI.Examples
{
    /// <summary>
    /// store — create a KV store, read it back, delete it.
    /// Operations: post_provisioning_kv, get_provisioning_kv_by_name,
    /// delete_provisioning_kv_by_name. The delete is in `finally`, so a failed read
    /// still cleans up.
    /// </summary>
    public static class Store
    {
        public static int Run()
        {
            var kv = new ProvisioningApi(Gateway.Configuration());
            // Names are org-unique, so each run makes a fresh one.
            var name = "sdk-example-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                var created = kv.PostProvisioningKv(new ProvisioningProvisionRequest(name: name));
                Console.WriteLine($"created  {created.Name} ({created.Status})");
                var read = kv.GetProvisioningKvByName(name);
                Console.WriteLine($"read     {read.Name} host={read.Host} port={read.Port} status={read.Status}");
                return 0;
            }
            catch (ApiException e)
            {
                Console.Error.WriteLine($"kv failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                return 1;
            }
            finally
            {
                try
                {
                    kv.DeleteProvisioningKvByName(name);
                    Console.WriteLine($"deleted  {name}");
                }
                catch (ApiException e)
                {
                    Console.Error.WriteLine($"delete {name} failed: HTTP {e.ErrorCode} {e.ErrorContent}");
                }
            }
        }
    }
}
