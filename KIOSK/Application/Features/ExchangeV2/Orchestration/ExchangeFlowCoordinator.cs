using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.Application.Services.Exchange;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.Application.Services.Devices.IdScanner;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Kiosk.Application.Features.ExchangeV2.Orchestration;

public interface IExchangeFlowCoordinator
{
    event EventHandler<ExchangeFlowChangedEventArgs>? FlowChanged;
    event EventHandler<IdScannerEvent>? ScanProgressChanged;
    event EventHandler<ExchangeDepositProgressChangedEventArgs>? DepositProgressChanged;

    ExchangeFlowContext Context { get; }

    Task StartAsync(CancellationToken ct = default);
    Task ConfirmStartAsync(CancellationToken ct = default);
    Task SelectMethodAsync(ExchangeMethod method, CancellationToken ct = default);
    Task SelectCurrencyAsync(string currencyCode, decimal exchangeRate, CancellationToken ct = default);
    Task SetTermsAgreementAsync(bool isAgreed, CancellationToken ct = default);
    Task ConfirmConsentAsync(CancellationToken ct = default);
    Task<ExchangeScanSessionResult> RunScanAsync(TimeSpan timeout, CancellationToken ct = default);
    Task ProceedFromScanCompletedAsync(CancellationToken ct = default);
    Task ProceedFromDepositAsync(CancellationToken ct = default);
    Task CompleteExchangeAsync(bool printReceipt, CancellationToken ct = default);
    Task<BackNavigationResult> GoBackAsync(CancellationToken ct = default);
}

public sealed class ExchangeFlowCoordinator : IExchangeFlowCoordinator
{
    private readonly IExchangeScanSession _scanSession;
    private readonly IExchangeDepositSession _depositSession;
    private readonly IExchangeWithdrawalSession _withdrawalSession;
    private readonly IExchangeCashBalanceProvider _cashBalanceProvider;
    private readonly IDepositLimitProvider _depositLimitProvider;
    private readonly ILogger<ExchangeFlowCoordinator> _logger;

    public ExchangeFlowCoordinator(
        IExchangeScanSession scanSession,
        IExchangeDepositSession depositSession,
        IExchangeWithdrawalSession withdrawalSession,
        IExchangeCashBalanceProvider cashBalanceProvider,
        IDepositLimitProvider depositLimitProvider,
        ILogger<ExchangeFlowCoordinator> logger)
    {
        _scanSession = scanSession;
        _depositSession = depositSession;
        _withdrawalSession = withdrawalSession;
        _cashBalanceProvider = cashBalanceProvider;
        _depositLimitProvider = depositLimitProvider;
        _logger = logger;
        _scanSession.ProgressChanged += OnScanProgressChanged;
        _depositSession.ProgressChanged += OnDepositProgressChanged;
    }

    public event EventHandler<ExchangeFlowChangedEventArgs>? FlowChanged;
    public event EventHandler<IdScannerEvent>? ScanProgressChanged;
    public event EventHandler<ExchangeDepositProgressChangedEventArgs>? DepositProgressChanged;

    public ExchangeFlowContext Context { get; } = new();

    public Task StartAsync(CancellationToken ct = default)
    {
        Context.ResetForRestart();
        MoveTo(ExchangeStep.Start);
        return Task.CompletedTask;
    }

    public Task ConfirmStartAsync(CancellationToken ct = default)
    {
        EnsureStep(ExchangeStep.Start);
        MoveTo(ExchangeStep.MethodSelection);
        return Task.CompletedTask;
    }

    public Task SelectMethodAsync(ExchangeMethod method, CancellationToken ct = default)
    {
        EnsureStep(ExchangeStep.MethodSelection);
        Context.Method = method;
        MoveTo(ExchangeStep.CurrencySelection);
        return Task.CompletedTask;
    }

    public Task SelectCurrencyAsync(string currencyCode, decimal exchangeRate, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        EnsureStep(ExchangeStep.CurrencySelection);

        Context.SourceCurrencyCode = currencyCode;
        Context.ExchangeRate = exchangeRate;
        Context.IsTermsAgreed = false;
        MoveTo(ExchangeStep.Consent);
        return Task.CompletedTask;
    }

    public Task SetTermsAgreementAsync(bool isAgreed, CancellationToken ct = default)
    {
        EnsureStep(ExchangeStep.Consent);
        Context.IsTermsAgreed = isAgreed;
        return Task.CompletedTask;
    }

    public Task ConfirmConsentAsync(CancellationToken ct = default)
    {
        EnsureStep(ExchangeStep.Consent);

        if (!Context.IsTermsAgreed)
            throw new InvalidOperationException("Terms agreement is required before continuing.");

        MoveTo(ExchangeStep.ScanIntro);
        return Task.CompletedTask;
    }

    public async Task<ExchangeScanSessionResult> RunScanAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        EnsureStep(ExchangeStep.ScanIntro);

        MoveTo(ExchangeStep.Scanning);
        var result = await _scanSession.ExecuteAsync(timeout, ct).ConfigureAwait(false);
        if (!result.Success)
        {
            _logger.LogInformation(
                "Exchange scan failed. transactionId={TransactionId} code={Code}",
                Context.TransactionId,
                result.ErrorCode);

            Context.ScanCapture = result.Capture;
            Context.ScanOcr = result.Ocr;
            Context.ScanResultState = ScanResultState.Failed;
            Context.ScanErrorCode = result.ErrorCode;
            Context.ScanErrorMessage = result.ErrorMessage;
            MoveTo(ExchangeStep.ScanCompleted);
            return result;
        }

        Context.ScanCapture = result.Capture;
        Context.ScanOcr = result.Ocr;
        Context.ScanResultState = ScanResultState.Succeeded;
        Context.ScanErrorCode = null;
        Context.ScanErrorMessage = null;
        MoveTo(ExchangeStep.ScanCompleted);
        return result;
    }

    public async Task ProceedFromScanCompletedAsync(CancellationToken ct = default)
    {
        EnsureStep(ExchangeStep.ScanCompleted);

        if (Context.ScanResultState != ScanResultState.Succeeded)
            throw new InvalidOperationException("Deposit step can only start after a successful scan result.");

        var sourceCurrency = string.IsNullOrWhiteSpace(Context.SourceCurrencyCode)
            ? "usd"
            : Context.SourceCurrencyCode;

        Context.CashBalanceSlots = await _cashBalanceProvider.GetSlotsAsync(ct).ConfigureAwait(false);
        Context.DepositLimit = await _depositLimitProvider.GetDepositLimitAsync(sourceCurrency, ct).ConfigureAwait(false);
        Context.ExchangeRate = Context.ExchangeRate > 0m
            ? Context.ExchangeRate
            : ResolveExchangeRate(sourceCurrency);
        Context.DepositAmount = 0m;
        Context.PreviewExchangeAmount = 0m;
        Context.DepositStatusMessage = "외화를 투입해주세요.";

        MoveTo(ExchangeStep.Deposit);

        var start = await _depositSession.StartAsync(
            new ExchangeDepositSessionOptions(
                sourceCurrency,
                Context.TargetCurrencyCode,
                Context.ExchangeRate,
                Context.DepositLimit),
            ct).ConfigureAwait(false);

        if (!start.Success)
        {
            Context.DepositStatusMessage = start.ErrorMessage ?? "입금기를 시작하지 못했습니다.";
            DepositProgressChanged?.Invoke(
                this,
                new ExchangeDepositProgressChangedEventArgs(
                    Context.DepositAmount,
                    Context.PreviewExchangeAmount,
                    null,
                    sourceCurrency,
                    false,
                    Context.DepositStatusMessage));
        }
    }

    public async Task ProceedFromDepositAsync(CancellationToken ct = default)
    {
        EnsureStep(ExchangeStep.Deposit);
        await _depositSession.StopAsync(ct).ConfigureAwait(false);
        MoveTo(ExchangeStep.Dispensing);

        var result = await _withdrawalSession.ExecuteAsync(
            new ExchangeWithdrawalSessionOptions(
                Context.TargetCurrencyCode,
                Context.PreviewExchangeAmount,
                Context.CashBalanceSlots
                    .Select(x => new Kiosk.Application.Services.Devices.Withdrawal.WithdrawalSlotBalance(
                        x.Device,
                        x.Slot,
                        x.Currency,
                        x.Denomination,
                        x.Count))
                    .ToArray()),
            ct).ConfigureAwait(false);

        Context.WithdrawalRequestedAmount = result.RequestedAmount;
        Context.WithdrawalDispensedAmount = result.DispensedAmount;
        Context.WithdrawalRemainingAmount = Math.Max(0m, result.RequestedAmount - result.DispensedAmount);
        Context.WithdrawalAllocations = result.Allocations;
        Context.DispenseErrorCode = result.ErrorCode;
        Context.DispenseErrorMessage = result.ErrorMessage;
        Context.DispenseResultState = result.Success
            ? DispenseResultState.Succeeded
            : DispenseResultState.Failed;

        MoveTo(result.Success ? ExchangeStep.DispenseSuccess : ExchangeStep.DispenseFailure);
    }

    public Task CompleteExchangeAsync(bool printReceipt, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Exchange completed. transactionId={TransactionId} printReceipt={PrintReceipt}",
            Context.TransactionId,
            printReceipt);

        Context.ResetForRestart();
        MoveTo(ExchangeStep.Start);
        return Task.CompletedTask;
    }

    public async Task<BackNavigationResult> GoBackAsync(CancellationToken ct = default)
    {
        var policy = GetBackPolicy(Context.CurrentStep);
        return policy switch
        {
            BackNavigationPolicy.Allowed => GoBackAllowed(),
            BackNavigationPolicy.Blocked => BackNavigationResult.Blocked(Context.CurrentStep, "Back navigation is blocked in the current step."),
            BackNavigationPolicy.RequiresRestart => await RestartFromSafeStepAsync(ct).ConfigureAwait(false),
            _ => BackNavigationResult.Blocked(Context.CurrentStep, "Unknown back navigation policy.")
        };
    }

    private BackNavigationResult GoBackAllowed()
    {
        if (Context.CurrentStep == ExchangeStep.Deposit)
            _depositSession.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        var previousStep = Context.CurrentStep switch
        {
            ExchangeStep.MethodSelection => ExchangeStep.Start,
            ExchangeStep.CurrencySelection => ExchangeStep.MethodSelection,
            ExchangeStep.Consent => ExchangeStep.CurrencySelection,
            ExchangeStep.ScanIntro => ExchangeStep.Consent,
            ExchangeStep.Deposit => ExchangeStep.ScanCompleted,
            ExchangeStep.Dispensing => ExchangeStep.Deposit,
            ExchangeStep.DispenseSuccess => ExchangeStep.Deposit,
            ExchangeStep.DispenseFailure => ExchangeStep.Deposit,
            _ => Context.CurrentStep
        };

        MoveTo(previousStep);
        return BackNavigationResult.Allowed(Context.CurrentStep);
    }

    private async Task<BackNavigationResult> RestartFromSafeStepAsync(CancellationToken ct)
    {
        await _scanSession.StopAsync(ct).ConfigureAwait(false);
        Context.ResetScan();
        MoveTo(ExchangeStep.ScanIntro);
        return BackNavigationResult.Restarted(
            Context.CurrentStep,
            "Current step requires scan restart. The scan result was discarded.");
    }

    private BackNavigationPolicy GetBackPolicy(ExchangeStep step)
        => step switch
        {
            ExchangeStep.Start => BackNavigationPolicy.Allowed,
            ExchangeStep.MethodSelection => BackNavigationPolicy.Allowed,
            ExchangeStep.CurrencySelection => BackNavigationPolicy.Allowed,
            ExchangeStep.Consent => BackNavigationPolicy.Allowed,
            ExchangeStep.ScanIntro => BackNavigationPolicy.Allowed,
            ExchangeStep.Scanning => BackNavigationPolicy.RequiresRestart,
            ExchangeStep.ScanCompleted => BackNavigationPolicy.RequiresRestart,
            ExchangeStep.Deposit => BackNavigationPolicy.Allowed,
            ExchangeStep.Dispensing => BackNavigationPolicy.Blocked,
            ExchangeStep.DispenseSuccess => BackNavigationPolicy.Blocked,
            ExchangeStep.DispenseFailure => BackNavigationPolicy.Blocked,
            _ => BackNavigationPolicy.Blocked
        };

    private void MoveTo(ExchangeStep step)
    {
        Context.CurrentStep = step;
        FlowChanged?.Invoke(this, new ExchangeFlowChangedEventArgs(step, Context));
    }

    private void OnScanProgressChanged(object? sender, IdScannerEvent e)
    {
        ScanProgressChanged?.Invoke(this, e);
    }

    private void OnDepositProgressChanged(object? sender, ExchangeDepositProgressChangedEventArgs e)
    {
        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(() => OnDepositProgressChanged(sender, e));
            return;
        }

        Context.DepositAmount = e.ApprovedDepositAmount;
        Context.PreviewExchangeAmount = e.ExchangedAmount;
        Context.DepositStatusMessage = e.StatusMessage;
        DepositProgressChanged?.Invoke(this, e);
    }

    private void EnsureStep(ExchangeStep expected)
    {
        if (Context.CurrentStep != expected)
        {
            throw new InvalidOperationException(
                $"Invalid exchange step transition. Expected '{expected}', current '{Context.CurrentStep}'.");
        }
    }

    private static decimal ResolveExchangeRate(string currencyCode)
        => currencyCode.Trim().ToUpperInvariant() switch
        {
            "USD" => 1470m,
            _ => 0m
        };
}
