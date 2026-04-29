namespace Kiosk.Infrastructure.Integrations.Cems.Models;

public sealed record CemsCommandResult<T>(
    bool Success,
    T? Data,
    CemsError? Error,
    int? HttpStatusCode,
    string RawBody,
    string CorrelationId);

public sealed record CemsError(
    string Code,
    string Message,
    bool Retryable);
