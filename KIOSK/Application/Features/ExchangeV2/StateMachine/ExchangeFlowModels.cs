using Kiosk.Application.Services.Devices.IdScanner;

namespace Kiosk.Application.Features.ExchangeV2.StateMachine;

public enum ExchangeStep
{
    Idle,
    LanguageSelection,
    Start,
    MethodSelection,
    CurrencySelection,
    Consent,
    ScanIntro,
    Scanning,
    ScanCompleted,
    Deposit,
    Dispensing,
    DispenseSuccess,
    DispenseFailure
}

public enum ExchangeMethod
{
    PrepaidCard,
    Cash
}

public enum BackNavigationPolicy
{
    Allowed,
    Blocked,
    RequiresRestart
}

public enum ScanResultState
{
    None,
    Succeeded,
    Failed
}

public enum DispenseResultState
{
    None,
    Succeeded,
    Failed
}

public sealed class ExchangeFlowContext
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString("N");
    public ExchangeMethod? Method { get; set; }
    public string? SourceCurrencyCode { get; set; }
    public string TargetCurrencyCode { get; set; } = "krw";
    public bool IsTermsAgreed { get; set; }
    public ExchangeStep CurrentStep { get; set; } = ExchangeStep.Idle;
    public ScanCaptureResult? ScanCapture { get; set; }
    public ScanOcrResult? ScanOcr { get; set; }
    public ScanResultState ScanResultState { get; set; } = ScanResultState.None;
    public string? ScanErrorCode { get; set; }
    public string? ScanErrorMessage { get; set; }
    public IReadOnlyList<Kiosk.Application.Services.Exchange.CashBalanceSlot> CashBalanceSlots { get; set; } = [];
    public Kiosk.Application.Services.Exchange.DepositLimitSnapshot? DepositLimit { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal PreviewExchangeAmount { get; set; }
    public string? DepositStatusMessage { get; set; }
    public DispenseResultState DispenseResultState { get; set; } = DispenseResultState.None;
    public string? DispenseErrorCode { get; set; }
    public string? DispenseErrorMessage { get; set; }
    public decimal WithdrawalRequestedAmount { get; set; }
    public decimal WithdrawalDispensedAmount { get; set; }
    public decimal WithdrawalRemainingAmount { get; set; }
    public IReadOnlyList<Kiosk.Application.Services.Devices.Withdrawal.WithdrawalAllocation> WithdrawalAllocations { get; set; } = [];

    public void ResetScan()
    {
        ScanCapture = null;
        ScanOcr = null;
        ScanResultState = ScanResultState.None;
        ScanErrorCode = null;
        ScanErrorMessage = null;
    }

    public void ResetForRestart()
    {
        TransactionId = Guid.NewGuid().ToString("N");
        Method = null;
        SourceCurrencyCode = null;
        TargetCurrencyCode = "krw";
        IsTermsAgreed = false;
        CurrentStep = ExchangeStep.Idle;
        CashBalanceSlots = [];
        DepositLimit = null;
        ExchangeRate = 0m;
        DepositAmount = 0m;
        PreviewExchangeAmount = 0m;
        DepositStatusMessage = null;
        DispenseResultState = DispenseResultState.None;
        DispenseErrorCode = null;
        DispenseErrorMessage = null;
        WithdrawalRequestedAmount = 0m;
        WithdrawalDispensedAmount = 0m;
        WithdrawalRemainingAmount = 0m;
        WithdrawalAllocations = [];
        ResetScan();
    }
}

public sealed record ExchangeFlowChangedEventArgs(
    ExchangeStep Step,
    ExchangeFlowContext Context);

public sealed record BackNavigationResult(
    bool Success,
    ExchangeStep CurrentStep,
    string? Message = null)
{
    public static BackNavigationResult Allowed(ExchangeStep step)
        => new(true, step);

    public static BackNavigationResult Blocked(ExchangeStep step, string message)
        => new(false, step, message);

    public static BackNavigationResult Restarted(ExchangeStep step, string message)
        => new(true, step, message);
}
