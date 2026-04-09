using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.Orchestration;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.ViewModels.BottomActions;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels;

public sealed class ExchangeScreenFactory : IExchangeScreenFactory
{
    private readonly IExchangeFlowCoordinator _coordinator;
    private readonly IExchangeOptionProvider _optionProvider;

    public ExchangeScreenFactory(
        IExchangeFlowCoordinator coordinator,
        IExchangeOptionProvider optionProvider)
    {
        _coordinator = coordinator;
        _optionProvider = optionProvider;
    }

    public ExchangeStepViewModelBase CreateStepViewModel(
        ExchangeStep step,
        ExchangeFlowContext context,
        Func<Task> showModalAsync)
        => step switch
        {
            ExchangeStep.Idle => new MessageStepViewModel("환전", "초기 화면을 준비하고 있습니다."),
            ExchangeStep.Start => new ExchangeStartStepViewModel(() => _coordinator.ConfirmStartAsync()),
            ExchangeStep.MethodSelection => new MethodSelectionStepViewModel(
                new AsyncRelayCommand(() => _coordinator.SelectMethodAsync(ExchangeMethod.PrepaidCard)),
                new AsyncRelayCommand(() => _coordinator.SelectMethodAsync(ExchangeMethod.Cash))),
            ExchangeStep.CurrencySelection => new CurrencySelectionStepViewModel(_optionProvider.CreateCurrencyOptions()),
            ExchangeStep.Consent => new ConsentStepViewModel(
                new AsyncRelayCommand(showModalAsync))
            {
                IsAgreed = context.IsTermsAgreed
            },
            ExchangeStep.ScanIntro => new ScanIntroStepViewModel(),
            ExchangeStep.Scanning => new ScanningStepViewModel(),
            ExchangeStep.ScanCompleted => new ScanCompletedStepViewModel(
                context.ScanResultState == ScanResultState.Succeeded,
                context.ScanOcr?.DocumentType,
                context.ScanOcr?.Fields,
                context.ScanErrorMessage),
            ExchangeStep.Deposit => new DepositStepViewModel(
                context.SourceCurrencyCode ?? "usd",
                context.TargetCurrencyCode,
                context.DepositAmount,
                context.PreviewExchangeAmount,
                context.ExchangeRate,
                context.DepositLimit),
            ExchangeStep.Dispensing => new DispensingStepViewModel(
                context.TargetCurrencyCode,
                context.PreviewExchangeAmount,
                context.CashBalanceSlots
                    .Select(x => new Kiosk.Application.Services.Devices.Withdrawal.WithdrawalSlotBalance(
                        x.Device,
                        x.Slot,
                        x.Currency,
                        x.Denomination,
                        x.Count))
                    .ToArray()),
            ExchangeStep.DispenseSuccess => new DispenseSuccessStepViewModel(
                context.SourceCurrencyCode ?? "usd",
                context.TargetCurrencyCode,
                context.ExchangeRate,
                context.DepositAmount,
                context.WithdrawalRequestedAmount,
                context.WithdrawalDispensedAmount,
                context.WithdrawalAllocations,
                context.CashBalanceSlots),
            ExchangeStep.DispenseFailure => new DispenseFailureStepViewModel(
                context.SourceCurrencyCode ?? "usd",
                context.TargetCurrencyCode,
                context.DepositAmount,
                context.WithdrawalRequestedAmount,
                context.WithdrawalDispensedAmount,
                context.DispenseErrorMessage),
            _ => new MessageStepViewModel("환전", "지원되지 않는 단계입니다.")
        };

    public BottomActionViewModelBase? CreateBottomAction(
        ExchangeStep step,
        ExchangeFlowContext context,
        ExchangeStepViewModelBase? stepViewModel,
        IAsyncRelayCommand homeCommand)
        => step switch
        {
            ExchangeStep.Start => new BackOnlyActionViewModel(homeCommand, true),
            ExchangeStep.MethodSelection => new BackOnlyActionViewModel(new AsyncRelayCommand(() => _coordinator.GoBackAsync()), true),
            ExchangeStep.CurrencySelection => new BackOnlyActionViewModel(new AsyncRelayCommand(() => _coordinator.GoBackAsync()), true),
            ExchangeStep.Consent => new BackAndPrimaryActionViewModel(
                new AsyncRelayCommand(() => _coordinator.GoBackAsync()),
                true,
                new AsyncRelayCommand(() => _coordinator.ConfirmConsentAsync()),
                "다음",
                context.IsTermsAgreed),
            ExchangeStep.ScanIntro => new BackAndPrimaryActionViewModel(
                new AsyncRelayCommand(() => _coordinator.GoBackAsync()),
                true,
                new AsyncRelayCommand(() => _coordinator.RunScanAsync(TimeSpan.FromSeconds(20))),
                "다음",
                stepViewModel is IScanIntroStepViewModel scanIntro && scanIntro.CanProceed),
            ExchangeStep.Scanning => new BackOnlyActionViewModel(new AsyncRelayCommand(() => _coordinator.GoBackAsync()), true),
            ExchangeStep.ScanCompleted => context.ScanResultState == ScanResultState.Failed
                ? new PrimaryOnlyActionViewModel(
                    new AsyncRelayCommand(() => _coordinator.GoBackAsync()),
                    "다시하기")
                : new BackAndPrimaryActionViewModel(
                    new AsyncRelayCommand(() => _coordinator.GoBackAsync()),
                    true,
                    new AsyncRelayCommand(() => _coordinator.ProceedFromScanCompletedAsync()),
                    "다음",
                    true),
            ExchangeStep.Deposit => new BackAndPrimaryActionViewModel(
                new AsyncRelayCommand(() => _coordinator.GoBackAsync()),
                true,
                new AsyncRelayCommand(() => _coordinator.ProceedFromDepositAsync()),
                "다음",
                true),
            ExchangeStep.Dispensing => null,
            ExchangeStep.DispenseSuccess => new BackAndPrimaryActionViewModel(
                new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(false)),
                true,
                new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(true)),
                "영수증 출력",
                true,
                "영수증 미출력"),
            ExchangeStep.DispenseFailure => new PrimaryOnlyActionViewModel(
                new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(true)),
                "영수증 출력"),
            _ => null
        };

    public IReadOnlyList<ExchangeProgressStepViewModel> CreateProgressSteps(ExchangeStep step)
    {
        var progressStage = step switch
        {
            ExchangeStep.CurrencySelection => 1,
            ExchangeStep.Consent or ExchangeStep.ScanIntro or ExchangeStep.Scanning or ExchangeStep.ScanCompleted => 2,
            ExchangeStep.Deposit or ExchangeStep.Dispensing => 3,
            _ => 0
        };

        if (progressStage == 0)
            return [];

        return
        [
            CreateProgressStep("1", "통화 선택", progressStage == 1, progressStage > 1),
            CreateProgressStep("2", "신분증 스캔", progressStage == 2, progressStage > 2),
            CreateProgressStep("3", "외화 투입", progressStage == 3, progressStage > 3),
            CreateProgressStep("4", "원화 수령", progressStage == 4, false)
        ];
    }

    public bool ShouldShowStepHeader(ExchangeStep step)
        => step is ExchangeStep.CurrencySelection or ExchangeStep.Consent or ExchangeStep.ScanIntro or ExchangeStep.Scanning or ExchangeStep.ScanCompleted or ExchangeStep.Deposit or ExchangeStep.Dispensing;

    public bool ShouldUseFeatureBackground(ExchangeStep step)
        => step is ExchangeStep.CurrencySelection
            or ExchangeStep.Consent
            or ExchangeStep.ScanIntro
            or ExchangeStep.Scanning
            or ExchangeStep.ScanCompleted
            or ExchangeStep.Deposit
            or ExchangeStep.Dispensing
            or ExchangeStep.DispenseSuccess
            or ExchangeStep.DispenseFailure;

    public bool ShouldCollapseShellChrome(ExchangeStep step)
        => step is ExchangeStep.DispenseSuccess or ExchangeStep.DispenseFailure;

    private static ExchangeProgressStepViewModel CreateProgressStep(
        string numberText,
        string label,
        bool isActive,
        bool isComplete)
        => new(numberText, label)
        {
            IsActive = isActive,
            IsComplete = isComplete
        };
}
