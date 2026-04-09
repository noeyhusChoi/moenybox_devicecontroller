namespace Kiosk.Application.Models.Exchange;

public sealed record ExchangeLimitCheckResult(
    bool IsAllowed,
    decimal? RemainingAmount,
    string Provider,
    string? ReasonCode,
    string? Message);
