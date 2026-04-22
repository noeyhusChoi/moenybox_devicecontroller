namespace Kiosk.Infrastructure.Integrations.Common;

public sealed record HttpExecutionOptions(
    TimeSpan Timeout,
    int MaxRetry,
    string CorrelationId,
    IReadOnlyDictionary<string, string>? Headers = null);
