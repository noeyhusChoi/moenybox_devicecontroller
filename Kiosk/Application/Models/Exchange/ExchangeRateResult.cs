namespace Kiosk.Application.Models.Exchange;

public sealed record ExchangeRateResult(
    string CurrencyCode,
    decimal? Rate,
    DateTimeOffset FetchedAt,
    string Provider,
    string? Message);
