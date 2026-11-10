using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hanzo.AI.Client;
using Hanzo.AI.Model;

namespace Hanzo.AI.Api
{
    public partial class AgentApi
    {
        /// <summary>
        /// Run one of your org's agents with <paramref name="input"/> and return the recorded run.
        /// </summary>
        /// <remarks>
        /// <c>POST /v1/agent/{ref}/run</c> requires a JSON body <c>{"input": "..."}</c> on the wire
        /// even though the current OpenAPI emission omits <c>requestBody</c> on <c>post_agent_by_ref_run</c>.
        /// Keeping this typed overload in a non-generated partial file beside <see cref="Hanzo.AI.Gateway"/>
        /// preserves <c>scripts/generate.sh --check</c> while giving C# callers a working run call.
        /// </remarks>
        public AgentAgentRunView PostAgentByRefRun(string varRef, string input)
        {
            return PostAgentByRefRunWithHttpInfo(varRef, input).Data;
        }

        /// <summary>
        /// Run one of your org's agents with <paramref name="input"/> and return the HTTP response wrapper.
        /// </summary>
        public ApiResponse<AgentAgentRunView> PostAgentByRefRunWithHttpInfo(string varRef, string input)
        {
            if (varRef == null)
            {
                throw new ApiException(400, "Missing required parameter 'varRef' when calling AgentApi->PostAgentByRefRun");
            }

            var options = new RequestOptions();
            options.HeaderParameters.Add("Content-Type", "application/json");
            options.HeaderParameters.Add("Accept", "application/json");
            options.PathParameters.Add("ref", ClientUtils.ParameterToString(varRef));
            options.Data = new Dictionary<string, string> { ["input"] = input ?? string.Empty };

            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !options.HeaderParameters.ContainsKey("Authorization"))
            {
                options.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            var response = this.Client.Post<AgentAgentRunView>("/v1/agent/{ref}/run", options, this.Configuration);
            if (this.ExceptionFactory != null)
            {
                Exception exception = this.ExceptionFactory("PostAgentByRefRun", response);
                if (exception != null)
                {
                    throw exception;
                }
            }
            return response;
        }

        /// <summary>
        /// Asynchronously run one of your org's agents with <paramref name="input"/>.
        /// </summary>
        public async Task<AgentAgentRunView> PostAgentByRefRunAsync(string varRef, string input, CancellationToken cancellationToken = default)
        {
            var response = await PostAgentByRefRunWithHttpInfoAsync(varRef, input, cancellationToken).ConfigureAwait(false);
            return response.Data;
        }

        /// <summary>
        /// Asynchronously run one of your org's agents with <paramref name="input"/> and return the HTTP response wrapper.
        /// </summary>
        public async Task<ApiResponse<AgentAgentRunView>> PostAgentByRefRunWithHttpInfoAsync(string varRef, string input, CancellationToken cancellationToken = default)
        {
            if (varRef == null)
            {
                throw new ApiException(400, "Missing required parameter 'varRef' when calling AgentApi->PostAgentByRefRun");
            }

            var options = new RequestOptions();
            options.HeaderParameters.Add("Content-Type", "application/json");
            options.HeaderParameters.Add("Accept", "application/json");
            options.PathParameters.Add("ref", ClientUtils.ParameterToString(varRef));
            options.Data = new Dictionary<string, string> { ["input"] = input ?? string.Empty };

            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !options.HeaderParameters.ContainsKey("Authorization"))
            {
                options.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            var response = await this.AsynchronousClient.PostAsync<AgentAgentRunView>("/v1/agent/{ref}/run", options, this.Configuration, cancellationToken).ConfigureAwait(false);
            if (this.ExceptionFactory != null)
            {
                Exception exception = this.ExceptionFactory("PostAgentByRefRun", response);
                if (exception != null)
                {
                    throw exception;
                }
            }
            return response;
        }
    }
}
