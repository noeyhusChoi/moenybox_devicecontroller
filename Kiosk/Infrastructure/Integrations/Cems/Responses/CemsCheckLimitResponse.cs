namespace Kiosk.Infrastructure.Integrations.Cems.Responses;

public sealed record CemsCheckLimitResponse(
    bool Result,
    string? ErrorCode,
    decimal? LimitAmount,
    decimal? UsedAmount,
    IReadOnlyDictionary<string, string?> Fields);
