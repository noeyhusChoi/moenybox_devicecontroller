namespace Kiosk.Infrastructure.Integrations.Cems.Responses;

public sealed record CurrencyRate(
    decimal? Base,
    decimal? Sell,
    decimal? Buy);

public sealed record CemsGetRateAllResponse(
    bool Result,
    string? ErrorCode,
    IReadOnlyDictionary<string, CurrencyRate> Rates,
    IReadOnlyDictionary<string, string?> Fields);

public sealed record CemsRegisterTransactionResponse(
    bool Result,
    string? ErrorCode,
    IReadOnlyDictionary<string, string?> Fields);

public sealed record CemsSetCashResponse(
    bool Result,
    string? ErrorCode,
    IReadOnlyDictionary<string, string?> Fields);

public sealed record CemsPullCashResponse(
    bool Result,
    string? ErrorCode,
    IReadOnlyDictionary<string, string?> Fields);

public sealed record CemsIncidentResponse(
    bool Result,
    string? ErrorCode,
    IReadOnlyDictionary<string, string?> Fields);

public sealed record CemsSmsResponse(
    bool Result,
    string? ErrorCode,
    IReadOnlyDictionary<string, string?> Fields);
