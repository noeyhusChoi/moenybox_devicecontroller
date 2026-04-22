namespace Kiosk.Application.Models.Common;

public sealed record OperationResult<T>(
    bool Success,
    T? Data,
    AppError? Error,
    string Provider,
    string CorrelationId)
{
    public static OperationResult<T> FromSuccess(T data, string provider, string correlationId)
        => new(true, data, null, provider, correlationId);

    public static OperationResult<T> FromError(AppError error, string provider, string correlationId)
        => new(false, default, error, provider, correlationId);
}
