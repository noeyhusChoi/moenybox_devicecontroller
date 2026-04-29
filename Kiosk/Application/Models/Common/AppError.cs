namespace Kiosk.Application.Models.Common;

public sealed record AppError(
    string Code,
    string Message,
    string Category,
    bool Retryable,
    string Provider);
