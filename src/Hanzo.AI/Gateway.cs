using System;
using Hanzo.AI.Client;

namespace Hanzo.AI
{
    /// <summary>
    /// Endpoint, credential and tenant, resolved once.
    /// </summary>
    /// <remarks>
    /// Everything under Hanzo.AI.Api, .Client and .Model is generated from the
    /// API document cloud publishes, by scripts/generate.sh. This file is not: it
    /// is the hand-written entry point and the only place in the package that
    /// reads the environment.
    ///
    /// The credential is the generated client's. The document declares a
    /// <c>bearer</c> scheme on every operation except those marked
    /// <c>security: []</c>, and each generated call that needs it sends
    /// <see cref="Configuration.AccessToken"/> as <c>Authorization: Bearer</c>.
    /// <c>X-Org-Id</c> is a default header because it selects a tenant rather
    /// than authenticating one: no scheme declares it.
    /// <code>
    /// var keys = new AccountApi(Gateway.Configuration()).GetAccountKeys();
    /// </code>
    /// </remarks>
    public static class Gateway
    {
        /// <summary>The Hanzo gateway. Every route is <c>&lt;base&gt;/v1/&lt;service&gt;/...</c>.</summary>
        public const string DefaultBaseUrl = "https://api.hanzo.ai";

        /// <summary>
        /// A configuration from the environment: <c>HANZO_API_KEY</c>,
        /// <c>HANZO_BASE_URL</c> (default <see cref="DefaultBaseUrl"/>) and
        /// <c>HANZO_ORG_ID</c>.
        /// </summary>
        public static Configuration Configuration()
        {
            return Configuration(Env("HANZO_API_KEY"), Env("HANZO_BASE_URL"), Env("HANZO_ORG_ID"));
        }

        /// <summary>
        /// A configuration for one tenant, for a program that serves more than one.
        /// </summary>
        /// <param name="apiKey">An IAM access token or a Cloud API key. Null or empty
        /// sends none: the API refusing is the answer, and the public routes still
        /// work without one.</param>
        /// <param name="baseUrl">The gateway; null or empty means <see cref="DefaultBaseUrl"/>.</param>
        /// <param name="orgId">Sent as <c>X-Org-Id</c>; null or empty sends none, and the
        /// tenant comes from the token's <c>owner</c> claim.</param>
        public static Configuration Configuration(string? apiKey, string? baseUrl, string? orgId)
        {
            var config = new Configuration
            {
                BasePath = string.IsNullOrEmpty(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/'),
            };
            if (!string.IsNullOrEmpty(apiKey))
            {
                config.AccessToken = apiKey;
            }
            if (!string.IsNullOrEmpty(orgId))
            {
                config.DefaultHeaders["X-Org-Id"] = orgId;
            }
            return config;
        }

        private static string? Env(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
