using Kiosk.Application.Services;
using Kiosk.Domain.Entities;

namespace Kiosk.Application.Models.Exchange;

public sealed record ExchangeTransactionRegistrationCommand(TransactionModelV2 Transaction);

public sealed record TransactionRegistrationResult(
    bool Registered,
    string? ReasonCode,
    string Provider,
    string? Message);

public sealed record CashSyncCommand(IReadOnlySet<WithdrawalCassette> Cassettes);

public sealed record CashSyncResult(
    bool Synced,
    string Provider,
    string? Message);

public sealed record CashStateResult(
    IReadOnlyDictionary<string, string?> Fields,
    string Provider,
    string? Message);

public sealed record CurrencyRate(
    decimal? Base,
    decimal? Sell,
    decimal? Buy);

public sealed record ExchangeRateListResult(
    IReadOnlyDictionary<string, CurrencyRate> Rates,
    string Provider,
    string? Message);

public sealed record IncidentReportCommand(
    DateTime OccurredAt,
    string Message);

public sealed record SmsCommand(
    DateTime OccurredAt,
    string Type,
    string Message);

public sealed record OperationAckResult(
    bool Accepted,
    string Provider,
    string? Message);
