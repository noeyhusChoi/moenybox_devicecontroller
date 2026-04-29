namespace Kiosk.Infrastructure.Integrations.Cems.Responses;

public sealed record CemsGetRateResponse(
    bool Result,
    string? ErrorCode,
    string? CurrencyCode,
    decimal? Rate,
    IReadOnlyDictionary<string, string?> Fields);
