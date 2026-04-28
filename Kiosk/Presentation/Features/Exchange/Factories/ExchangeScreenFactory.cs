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
            ExchangeStep.Idle => new ExchangeStartStepViewModel(() => _coordinator.ConfirmStartAsync()),
            ExchangeStep.Start => new ExchangeStartStepViewModel(() => _coordinator.ConfirmStartAsync()),
            ExchangeStep.MethodSelection => new MethodSelectionStepViewModel(
                new AsyncRelayCommand(() => _coordinator.SelectMethodAsync(ExchangeMethod.PrepaidCard)),
                new AsyncRelayCommand(() => _coordinator.SelectMethodAsync(ExchangeMethod.Cash))),
            ExchangeStep.CurrencySelection => new CurrencySelectionStepViewModel(_optionProvider.CreateCurrencyOptions(
                (code, rate) => _coordinator.SelectCurrencyAsync(code, rate),
                includeKrw: false)),
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
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unsupported exchange step.")
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
        stepViewModel.IsSecondaryEnabled = true;
        stepViewModel.PrimaryCommand = null;
        stepViewModel.IsPrimaryEnabled = true;

        switch (step)
        {
            case ExchangeStep.Start:
                stepViewModel.SecondaryCommand = homeCommand;
                break;

            case ExchangeStep.MethodSelection:
            case ExchangeStep.CurrencySelection:
            case ExchangeStep.Scanning:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                break;

            case ExchangeStep.Consent:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.ConfirmConsentAsync());
                stepViewModel.IsPrimaryEnabled = context.IsTermsAgreed;
                break;

            case ExchangeStep.ScanIntro:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.RunScanAsync(TimeSpan.FromSeconds(20)));
                stepViewModel.IsPrimaryEnabled = stepViewModel is IScanIntroStepViewModel scanIntro && scanIntro.CanProceed;
                break;

            case ExchangeStep.ScanCompleted:
                if (context.ScanResultState == ScanResultState.Failed)
                {
                    stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                }
                else
                {
                    stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                    stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.ProceedFromScanCompletedAsync());
                }
                break;

            case ExchangeStep.Deposit:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.GoBackAsync());
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.ProceedFromDepositAsync());
                break;

            case ExchangeStep.DispenseSuccess:
                stepViewModel.SecondaryCommand = new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(false));
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(true));
                break;

            case ExchangeStep.DispenseFailure:
                stepViewModel.PrimaryCommand = new AsyncRelayCommand(() => _coordinator.CompleteExchangeAsync(true));
                break;
        }
    }

    public int GetProgressStage(ExchangeStep step)
        => step switch
        {
            ExchangeStep.CurrencySelection => 1,
            ExchangeStep.Consent
                or ExchangeStep.ScanIntro
                or ExchangeStep.Scanning
                or ExchangeStep.ScanCompleted => 2,
            ExchangeStep.Deposit => 3,
            ExchangeStep.Dispensing => 4,
            _ => 0
        };

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

}
