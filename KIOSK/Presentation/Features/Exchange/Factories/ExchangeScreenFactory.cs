using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.Orchestration;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
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

    public void ConfigureStepActions(
        ExchangeStep step,
        ExchangeFlowContext context,
        ExchangeStepViewModelBase? stepViewModel,
        IAsyncRelayCommand homeCommand)
    {
        if (stepViewModel is null)
            return;

        stepViewModel.SecondaryCommand = null;
        stepViewModel.SecondaryText = null;
        stepViewModel.IsSecondaryEnabled = true;
        stepViewModel.PrimaryCommand = null;
        stepViewModel.PrimaryText = null;
        stepViewModel.IsPrimaryEnabled = true;

        switch (step)
        {
            case ExchangeStep.Start:
                stepViewModel.SecondaryCommand = homeCommand;
                stepViewModel.SecondaryText = "이전";
                break;

            case ExchangeStep.MethodSelection:
            case ExchangeStep.CurrencySelection:
            case ExchangeStep.Scanning:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                stepViewModel.SecondaryText = "이전";
                break;

            case ExchangeStep.Consent:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                stepViewModel.SecondaryText = "이전";
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.ConfirmConsentAsync());
                stepViewModel.PrimaryText = "다음";
                stepViewModel.IsPrimaryEnabled = context.IsTermsAgreed;
                break;

            case ExchangeStep.ScanIntro:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                stepViewModel.SecondaryText = "이전";
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.RunScanAsync(TimeSpan.FromSeconds(20)));
                stepViewModel.PrimaryText = "다음";
                stepViewModel.IsPrimaryEnabled = stepViewModel is IScanIntroStepViewModel scanIntro && scanIntro.CanProceed;
                break;

            case ExchangeStep.ScanCompleted:
                if (context.ScanResultState == ScanResultState.Failed)
                {
                    stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                    stepViewModel.PrimaryText = "다시하기";
                }
                else
                {
                    stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                    stepViewModel.SecondaryText = "이전";
                    stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.ProceedFromScanCompletedAsync());
                    stepViewModel.PrimaryText = "다음";
                }
                break;

            case ExchangeStep.Deposit:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                stepViewModel.SecondaryText = "이전";
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.ProceedFromDepositAsync());
                stepViewModel.PrimaryText = "다음";
                break;

            case ExchangeStep.DispenseSuccess:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(false));
                stepViewModel.SecondaryText = "영수증 미출력";
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(true));
                stepViewModel.PrimaryText = "영수증 출력";
                break;

            case ExchangeStep.DispenseFailure:
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(true));
                stepViewModel.PrimaryText = "영수증 출력";
                break;
        }
    }

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
            CreateProgressStep("3", "외화 입금", progressStage == 3, progressStage > 3),
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
