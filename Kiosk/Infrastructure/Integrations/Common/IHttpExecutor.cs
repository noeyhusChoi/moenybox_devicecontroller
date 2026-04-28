using System.Net.Http;

namespace Kiosk.Infrastructure.Integrations.Common;

public interface IHttpExecutor
{
    Task<HttpExecutionResult> SendAsync(
        HttpRequestMessage request,
        HttpExecutionOptions options,
        CancellationToken ct);
}
