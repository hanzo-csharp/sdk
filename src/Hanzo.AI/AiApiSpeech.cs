using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hanzo.AI.Client;

namespace Hanzo.AI.Api
{
    public partial class AiApi
    {
        /// <summary>
        /// Synthesize speech audio via <c>POST /v1/audio/speech</c> (default model <c>zen-voice</c>) and return raw audio bytes (MP3 by default).
        /// </summary>
        /// <remarks>
        /// Ready to mount directly onto Godot 4 .NET <c>new AudioStreamMP3 { Data = bytes }</c> or stream to disk.
        /// Kept in a non-generated partial file beside <see cref="Hanzo.AI.Gateway"/> so <c>scripts/generate.sh --check</c> stays clean.
        /// </remarks>
        public byte[] PostAudioSpeechBytes(string input, string voice = "alloy", string model = "zen-voice", string responseFormat = "mp3", double speed = 1.0)
        {
            return PostAudioSpeechBytesWithHttpInfo(input, voice, model, responseFormat, speed).Data;
        }

        /// <summary>
        /// Synthesize speech audio via <c>POST /v1/audio/speech</c> and return the HTTP response wrapper with raw audio bytes.
        /// </summary>
        public ApiResponse<byte[]> PostAudioSpeechBytesWithHttpInfo(string input, string voice = "alloy", string model = "zen-voice", string responseFormat = "mp3", double speed = 1.0)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ApiException(400, "Missing required parameter 'input' when calling AiApi->PostAudioSpeechBytes");
            }

            var options = new RequestOptions();
            options.HeaderParameters.Add("Content-Type", "application/json");
            options.HeaderParameters.Add("Accept", "audio/mpeg, application/octet-stream, application/json");
            options.Data = new Dictionary<string, object>
            {
                ["model"] = string.IsNullOrEmpty(model) ? "zen-voice" : model,
                ["input"] = input,
                ["voice"] = string.IsNullOrEmpty(voice) ? "alloy" : voice,
                ["response_format"] = string.IsNullOrEmpty(responseFormat) ? "mp3" : responseFormat,
                ["speed"] = speed,
            };

            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !options.HeaderParameters.ContainsKey("Authorization"))
            {
                options.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            var response = this.Client.Post<byte[]>("/v1/audio/speech", options, this.Configuration);
            if (this.ExceptionFactory != null)
            {
                Exception exception = this.ExceptionFactory("PostAudioSpeechBytes", response);
                if (exception != null)
                {
                    throw exception;
                }
            }
            return response;
        }

        /// <summary>
        /// Asynchronously synthesize speech audio via <c>POST /v1/audio/speech</c> (default model <c>zen-voice</c>) and return raw audio bytes.
        /// </summary>
        public async Task<byte[]> PostAudioSpeechBytesAsync(string input, string voice = "alloy", string model = "zen-voice", string responseFormat = "mp3", double speed = 1.0, CancellationToken cancellationToken = default)
        {
            var response = await PostAudioSpeechBytesWithHttpInfoAsync(input, voice, model, responseFormat, speed, cancellationToken).ConfigureAwait(false);
            return response.Data;
        }

        /// <summary>
        /// Asynchronously synthesize speech audio via <c>POST /v1/audio/speech</c> and return the HTTP response wrapper with raw audio bytes.
        /// </summary>
        public async Task<ApiResponse<byte[]>> PostAudioSpeechBytesWithHttpInfoAsync(string input, string voice = "alloy", string model = "zen-voice", string responseFormat = "mp3", double speed = 1.0, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ApiException(400, "Missing required parameter 'input' when calling AiApi->PostAudioSpeechBytes");
            }

            var options = new RequestOptions();
            options.HeaderParameters.Add("Content-Type", "application/json");
            options.HeaderParameters.Add("Accept", "audio/mpeg, application/octet-stream, application/json");
            options.Data = new Dictionary<string, object>
            {
                ["model"] = string.IsNullOrEmpty(model) ? "zen-voice" : model,
                ["input"] = input,
                ["voice"] = string.IsNullOrEmpty(voice) ? "alloy" : voice,
                ["response_format"] = string.IsNullOrEmpty(responseFormat) ? "mp3" : responseFormat,
                ["speed"] = speed,
            };

            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !options.HeaderParameters.ContainsKey("Authorization"))
            {
                options.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            var response = await this.AsynchronousClient.PostAsync<byte[]>("/v1/audio/speech", options, this.Configuration, cancellationToken).ConfigureAwait(false);
            if (this.ExceptionFactory != null)
            {
                Exception exception = this.ExceptionFactory("PostAudioSpeechBytes", response);
                if (exception != null)
                {
                    throw exception;
                }
            }
            return response;
        }
    }
}
