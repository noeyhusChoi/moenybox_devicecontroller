using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Kiosk.Infrastructure.Integrations.Common;

public sealed class HttpExecutor : IHttpExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpExecutor> _logger;

    public HttpExecutor(IHttpClientFactory httpClientFactory, ILogger<HttpExecutor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HttpExecutionResult> SendAsync(
        HttpRequestMessage request,
        HttpExecutionOptions options,
        CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            using var clonedRequest = await CloneAsync(request, ct);
            clonedRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", options.CorrelationId);

            if (options.Headers is not null)
            {
                foreach (var header in options.Headers)
                    clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(options.Timeout);

            try
            {
                var client = _httpClientFactory.CreateClient("ExternalApi");
                using var response = await client.SendAsync(clonedRequest, timeoutCts.Token);
                var rawBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                var success = response.IsSuccessStatusCode;

                if (success || attempt > options.MaxRetry || (int)response.StatusCode < 500)
                {
                    return new HttpExecutionResult(
                        success,
                        (int)response.StatusCode,
                        rawBody,
                        null,
                        options.CorrelationId);
                }
            }
            catch (Exception ex) when (attempt <= options.MaxRetry)
            {
                _logger.LogWarning(ex, "External API request retry. correlationId={CorrelationId} attempt={Attempt}",
                    options.CorrelationId, attempt);
            }
            catch (Exception ex)
            {
                return new HttpExecutionResult(
                    false,
                    null,
                    string.Empty,
                    ex,
                    options.CorrelationId);
            }
        }
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(ct);
            var contentClone = new ByteArrayContent(bytes);

            foreach (var header in request.Content.Headers)
                contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            clone.Content = contentClone;
        }

        return clone;
    }
}
