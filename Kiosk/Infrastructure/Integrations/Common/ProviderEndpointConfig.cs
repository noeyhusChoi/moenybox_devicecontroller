namespace Kiosk.Infrastructure.Integrations.Common;

public sealed record ProviderEndpointConfig(
    string ProviderName,
    string BaseUrl,
    string ApiKey,
    TimeSpan Timeout,
    int RetryCount);
