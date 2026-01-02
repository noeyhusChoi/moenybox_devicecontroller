using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.API.Core
{
    public interface IApiClient
    {
        Task<(TResponse parsed, ApiSendResult raw)> SendOnceAsync<TResponse>(
            ApiEnvelope env,
            Func<string, TResponse> parser,
            CancellationToken ct);

        Task<(TResponse parsed, ApiSendResult raw)> SendWithRetryAsync<TResponse>(
            ApiEnvelope env,
            Func<string, TResponse> parser,
            int maxRetry,
            CancellationToken ct);
    }

    public sealed class ApiClient : IApiClient
    {
        private readonly IApiGateway _gateway;

        public ApiClient(IApiGateway gateway)
        {
            _gateway = gateway;
        }

        public async Task<(TResponse parsed, ApiSendResult raw)> SendOnceAsync<TResponse>(
            ApiEnvelope env, Func<string, TResponse> parser, CancellationToken ct)
        {
            var raw = await _gateway.SendAsync(env, ct);
            var parsed = parser(raw.Raw);
            return (parsed, raw);
        }

        public async Task<(TResponse parsed, ApiSendResult raw)> SendWithRetryAsync<TResponse>(
            ApiEnvelope env, Func<string, TResponse> parser, int maxRetry, CancellationToken ct)
        {
            int attempt = 0;
            ApiSendResult lastRaw = new(false, 0, string.Empty, "NotSent");
            Exception? lastEx = null;

            while (true)
            {
                try
                {
                    lastRaw = await _gateway.SendAsync(env, ct);
                    var parsed = parser(lastRaw.Raw);

                    if (lastRaw.Success)
                        return (parsed, lastRaw);

                    if (attempt >= maxRetry)
                        return (parsed, lastRaw);
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    if (attempt >= maxRetry)
                        throw;
                }

                attempt++;
                await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt), ct);
            }
        }
    }
}
