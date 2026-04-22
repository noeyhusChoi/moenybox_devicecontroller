using Kiosk.Application.Services;
using Kiosk.Domain.Entities;

namespace Kiosk.Infrastructure.Integrations.Cems.Requests;

public sealed record CemsGetRateAllRequest;

public sealed record CemsRegisterTransactionRequest(TransactionModelV2 Transaction);

public sealed record CemsSetCashRequest(IReadOnlySet<WithdrawalCassette> Cassettes);

public sealed record CemsPullCashRequest;

public sealed record CemsReportErrorRequest(
    DateTime OccurredAt,
    string Message);

public sealed record CemsSendSmsRequest(
    DateTime OccurredAt,
    string Type,
    string Message);
