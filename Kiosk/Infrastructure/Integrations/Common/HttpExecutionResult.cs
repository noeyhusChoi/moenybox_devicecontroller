namespace Kiosk.Infrastructure.Integrations.Common;

public sealed record HttpExecutionResult(
    bool Success,
    int? StatusCode,
    string RawBody,
    Exception? Exception,
    string CorrelationId);
