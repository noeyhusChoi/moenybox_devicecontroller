using Kiosk.Application.Contracts;
using Kiosk.Application.Models.Common;
using Kiosk.Application.Models.Exchange;
using Kiosk.Infrastructure.Integrations.Cems;
using Kiosk.Infrastructure.Integrations.Cems.Models;
using Kiosk.Infrastructure.Integrations.Cems.Requests;
using Kiosk.Infrastructure.Integrations.Cems.Responses;
using AppCurrencyRate = Kiosk.Application.Models.Exchange.CurrencyRate;

namespace Kiosk.Application.Services.Exchange;

public sealed class ExchangeService : IExchangeService
{
    private readonly ICemsClient _cemsClient;

    public ExchangeService(ICemsClient cemsClient)
    {
        _cemsClient = cemsClient;
    }

    public async Task<OperationResult<ExchangeRateResult>> GetRateAsync(string currencyCode, CancellationToken ct = default)
    {
        var result = await _cemsClient.GetRateAsync(new CemsGetRateRequest(currencyCode), ct);
        if (!result.Success || result.Data is null)
            return Error<ExchangeRateResult, CemsGetRateResponse>(result, "Failed to fetch exchange rate.");

        var appResult = new ExchangeRateResult(
            result.Data.CurrencyCode ?? currencyCode,
            result.Data.Rate,
            DateTimeOffset.UtcNow,
            "CEMS",
            result.Data.ErrorCode);

        return OperationResult<ExchangeRateResult>.FromSuccess(appResult, "CEMS", result.CorrelationId);
    }

    public async Task<OperationResult<ExchangeRateListResult>> GetRateAllAsync(CancellationToken ct = default)
    {
        var result = await _cemsClient.GetRateAllAsync(new CemsGetRateAllRequest(), ct);
        if (!result.Success || result.Data is null)
            return Error<ExchangeRateListResult, CemsGetRateAllResponse>(result, "Failed to fetch exchange rate list.");

        var rates = result.Data.Rates.ToDictionary(
            static item => item.Key,
            static item => new AppCurrencyRate(item.Value.Base, item.Value.Sell, item.Value.Buy),
            StringComparer.OrdinalIgnoreCase);
        var appResult = new ExchangeRateListResult(rates, "CEMS", result.Data.ErrorCode);
        return OperationResult<ExchangeRateListResult>.FromSuccess(appResult, "CEMS", result.CorrelationId);
    }

    public async Task<OperationResult<ExchangeLimitCheckResult>> CheckLimitAsync(string customerNumber, CancellationToken ct = default)
    {
        var result = await _cemsClient.CheckLimitAsync(new CemsCheckLimitRequest(customerNumber), ct);
        if (!result.Success || result.Data is null)
            return Error<ExchangeLimitCheckResult, CemsCheckLimitResponse>(result, "Failed to check exchange limit.");

        decimal? remainingAmount = null;
        if (result.Data.LimitAmount.HasValue && result.Data.UsedAmount.HasValue)
            remainingAmount = result.Data.LimitAmount.Value - result.Data.UsedAmount.Value;

        var appResult = new ExchangeLimitCheckResult(
            result.Data.Result,
            remainingAmount,
            "CEMS",
            result.Data.ErrorCode,
            result.Data.ErrorCode);

        return OperationResult<ExchangeLimitCheckResult>.FromSuccess(appResult, "CEMS", result.CorrelationId);
    }

    public async Task<OperationResult<TransactionRegistrationResult>> RegisterTransactionAsync(
        ExchangeTransactionRegistrationCommand command,
        CancellationToken ct = default)
    {
        var result = await _cemsClient.RegisterTransactionAsync(new CemsRegisterTransactionRequest(command.Transaction), ct);
        if (!result.Success || result.Data is null)
            return Error<TransactionRegistrationResult, CemsRegisterTransactionResponse>(result, "Failed to register transaction.");

        var appResult = new TransactionRegistrationResult(result.Data.Result, result.Data.ErrorCode, "CEMS", result.Data.ErrorCode);
        return OperationResult<TransactionRegistrationResult>.FromSuccess(appResult, "CEMS", result.CorrelationId);
    }

    public async Task<OperationResult<CashSyncResult>> SetCashAsync(CashSyncCommand command, CancellationToken ct = default)
    {
        var result = await _cemsClient.SetCashAsync(new CemsSetCashRequest(command.Cassettes), ct);
        if (!result.Success || result.Data is null)
            return Error<CashSyncResult, CemsSetCashResponse>(result, "Failed to sync cash state.");

        return OperationResult<CashSyncResult>.FromSuccess(
            new CashSyncResult(result.Data.Result, "CEMS", result.Data.ErrorCode),
            "CEMS",
            result.CorrelationId);
    }

    public async Task<OperationResult<CashStateResult>> PullCashAsync(CancellationToken ct = default)
    {
        var result = await _cemsClient.PullCashAsync(new CemsPullCashRequest(), ct);
        if (!result.Success || result.Data is null)
            return Error<CashStateResult, CemsPullCashResponse>(result, "Failed to pull cash state.");

        return OperationResult<CashStateResult>.FromSuccess(
            new CashStateResult(result.Data.Fields, "CEMS", result.Data.ErrorCode),
            "CEMS",
            result.CorrelationId);
    }

    public async Task<OperationResult<OperationAckResult>> ReportErrorAsync(IncidentReportCommand command, CancellationToken ct = default)
    {
        var result = await _cemsClient.ReportErrorAsync(new CemsReportErrorRequest(command.OccurredAt, command.Message), ct);
        if (!result.Success || result.Data is null)
            return Error<OperationAckResult, CemsIncidentResponse>(result, "Failed to report incident.");

        return OperationResult<OperationAckResult>.FromSuccess(
            new OperationAckResult(result.Data.Result, "CEMS", result.Data.ErrorCode),
            "CEMS",
            result.CorrelationId);
    }

    public async Task<OperationResult<OperationAckResult>> SendSmsAsync(SmsCommand command, CancellationToken ct = default)
    {
        var result = await _cemsClient.SendSmsAsync(new CemsSendSmsRequest(command.OccurredAt, command.Type, command.Message), ct);
        if (!result.Success || result.Data is null)
            return Error<OperationAckResult, CemsSmsResponse>(result, "Failed to send SMS.");

        return OperationResult<OperationAckResult>.FromSuccess(
            new OperationAckResult(result.Data.Result, "CEMS", result.Data.ErrorCode),
            "CEMS",
            result.CorrelationId);
    }

    private static OperationResult<T> Error<T, TData>(CemsCommandResult<TData> result, string fallbackMessage)
    {
        var error = result.Error is null
            ? new AppError("UNKNOWN", fallbackMessage, "Unknown", false, "CEMS")
            : new AppError(result.Error.Code, result.Error.Message, "ProviderRejected", result.Error.Retryable, "CEMS");

        return OperationResult<T>.FromError(error, "CEMS", result.CorrelationId);
    }
}
