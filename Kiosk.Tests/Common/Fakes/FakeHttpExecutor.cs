using System.Net.Http;
using Kiosk.Infrastructure.Integrations.Common;

namespace Kiosk.Tests.Common.Fakes;

internal sealed class FakeHttpExecutor : IHttpExecutor
{
    public HttpMethod? LastMethod { get; private set; }
    public Uri? LastRequestUri { get; private set; }
    public string? LastRequestBody { get; private set; }
    public HttpExecutionOptions? LastOptions { get; private set; }
    public HttpExecutionResult NextResult { get; set; } = new(true, 200, string.Empty, null, "corr-default");

    public async Task<HttpExecutionResult> SendAsync(HttpRequestMessage request, HttpExecutionOptions options, CancellationToken ct)
    {
        LastMethod = request.Method;
        LastRequestUri = request.RequestUri;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(ct);
        LastOptions = options;
        return NextResult;
    }
}
