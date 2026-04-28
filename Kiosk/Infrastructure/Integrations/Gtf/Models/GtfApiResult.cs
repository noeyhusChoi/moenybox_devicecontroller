namespace Kiosk.Infrastructure.Integrations.Gtf.Models;

public sealed record GtfApiResult<T>(
    bool Success,
    T? Data,
    GtfError? Error,
    int? HttpStatusCode,
    string RawBody,
    string CorrelationId);

public sealed record GtfError(
    string Code,
    string Message,
    bool Retryable);
