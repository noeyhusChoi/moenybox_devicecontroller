using Kiosk.Application.Models.Common;
using Kiosk.Application.Models.Exchange;

namespace Kiosk.Application.Contracts;

public interface IExchangeService
{
    Task<OperationResult<ExchangeRateResult>> GetRateAsync(string currencyCode, CancellationToken ct = default);
    Task<OperationResult<ExchangeRateListResult>> GetRateAllAsync(CancellationToken ct = default);
    Task<OperationResult<ExchangeLimitCheckResult>> CheckLimitAsync(string customerNumber, CancellationToken ct = default);
    Task<OperationResult<TransactionRegistrationResult>> RegisterTransactionAsync(
        ExchangeTransactionRegistrationCommand command,
        CancellationToken ct = default);
    Task<OperationResult<CashSyncResult>> SetCashAsync(CashSyncCommand command, CancellationToken ct = default);
    Task<OperationResult<CashStateResult>> PullCashAsync(CancellationToken ct = default);
    Task<OperationResult<OperationAckResult>> ReportErrorAsync(IncidentReportCommand command, CancellationToken ct = default);
    Task<OperationResult<OperationAckResult>> SendSmsAsync(SmsCommand command, CancellationToken ct = default);
}
