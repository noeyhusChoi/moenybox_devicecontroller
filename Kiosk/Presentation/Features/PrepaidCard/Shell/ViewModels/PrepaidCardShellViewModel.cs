using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.Application.Services.Devices.IdScanner;
using Kiosk.Application.Services.Exchange;
using Kiosk.ViewModels.Overlays;
using Kiosk.ViewModels.PrepaidCard;
using Kiosk.ViewModels.Steps;
using System.ComponentModel;
using System.Windows;

namespace Kiosk.ViewModels;

public enum PrepaidCardEntrySource
{
    Home,
    ExchangeEntry
}

public enum PrepaidCardStep
{
    Guide,
    ServiceSelection,
    ChargeMethodSelection,
    WalletSelection,
    CardRecognition,
    PaymentMethodSelection,
    CurrencySelection,
    Consent,
    ScanIntro,
    Scanning,
    ScanCompleted,
    DepositGuide,
    Deposit,
    AmountEntry
}

public enum PrepaidCardServiceKind
{
    PurchaseAndCharge,
    ChargeExistingCard
}

public enum PrepaidCardChargeMethod
{
    BothWallets,
    PrepaidWalletOnly,
    TrafficWalletOnly
}

public partial class PrepaidCardShellViewModel : ObservableObject, IModalSourceViewModel, IProgressChromeShellViewModel
{
    private readonly IExchangeScanSession _scanSession;
    private readonly IExchangeDepositSession _depositSession;
    private readonly IDepositLimitProvider _depositLimitProvider;
    private readonly IExchangeOptionProvider _optionProvider;
    private PrepaidCardEntrySource _entrySource = PrepaidCardEntrySource.Home;
    private PrepaidCardServiceKind? _selectedService;
    private PrepaidCardChargeMethod? _selectedChargeMethod;
    private PrepaidCardFundingType? _selectedFundingType;
    private string? _selectedCurrencyCode;
    private decimal? _selectedCurrencyRate;
    private ExchangeScanSessionResult? _scanResult;
    private DepositLimitSnapshot? _depositLimit;
    private decimal _approvedDepositAmount;
    private decimal _approvedExchangeAmount;

    public event EventHandler? HomeRequested;
    public event EventHandler? EntryBackRequested;

    [ObservableProperty]
    private string timerText = "180";

    [ObservableProperty]
    private bool showStepHeader;

    [ObservableProperty]
    private bool collapseShellChrome;

    [ObservableProperty]
    private int currentProgressStage;

    [ObservableProperty]
    private PrepaidCardStep currentStep;

    [ObservableProperty]
    private ExchangeStepViewModelBase? currentStepViewModel;

    [ObservableProperty]
    private object? currentModalViewModel;

    public PrepaidCardShellViewModel(
        IExchangeScanSession scanSession,
        IExchangeDepositSession depositSession,
        IDepositLimitProvider depositLimitProvider,
        IExchangeOptionProvider optionProvider)
    {
        _scanSession = scanSession;
        _depositSession = depositSession;
        _depositLimitProvider = depositLimitProvider;
        _optionProvider = optionProvider;
        _scanSession.ProgressChanged += OnScanProgressChanged;
        _depositSession.ProgressChanged += OnDepositProgressChanged;
        ApplyState(PrepaidCardStep.Guide);
    }

    public async Task StartFlowAsync(PrepaidCardEntrySource entrySource)
    {
        await StopDepositIfRunningAsync();
        _entrySource = entrySource;
        ResetFlow();
        ApplyState(PrepaidCardStep.Guide);
    }

    private void ResetFlow()
    {
        _selectedService = null;
        _selectedChargeMethod = null;
        _selectedFundingType = null;
        _selectedCurrencyCode = null;
        _selectedCurrencyRate = null;
        _scanResult = null;
        _depositLimit = null;
        _approvedDepositAmount = 0m;
        _approvedExchangeAmount = 0m;
        CurrentModalViewModel = null;
    }

    private Task SelectServiceAsync(PrepaidCardServiceKind service)
    {
        _selectedService = service;
        ApplyState(service == PrepaidCardServiceKind.PurchaseAndCharge
            ? PrepaidCardStep.ChargeMethodSelection
            : PrepaidCardStep.WalletSelection);
        return Task.CompletedTask;
    }

    private Task SelectChargeMethodAsync(PrepaidCardChargeMethod method)
    {
        _selectedChargeMethod = method;
        if (method == PrepaidCardChargeMethod.TrafficWalletOnly)
        {
            ApplyState(_selectedService == PrepaidCardServiceKind.ChargeExistingCard
                ? PrepaidCardStep.CardRecognition
                : PrepaidCardStep.PaymentMethodSelection);
        }
        else
        {
            ApplyState(_selectedService == PrepaidCardServiceKind.ChargeExistingCard
                ? PrepaidCardStep.CardRecognition
                : PrepaidCardStep.CurrencySelection);
        }
        return Task.CompletedTask;
    }

    private Task ProceedFromCardRecognitionAsync()
    {
        ApplyState(_selectedChargeMethod == PrepaidCardChargeMethod.TrafficWalletOnly
            ? PrepaidCardStep.PaymentMethodSelection
            : PrepaidCardStep.CurrencySelection);
        return Task.CompletedTask;
    }

    private Task SelectTrafficPaymentMethodAsync(PrepaidCardFundingType fundingType)
    {
        _selectedFundingType = fundingType;

        if (fundingType == PrepaidCardFundingType.EasyPay)
        {
            _selectedCurrencyCode = null;
            _selectedCurrencyRate = null;
            ApplyState(PrepaidCardStep.AmountEntry);
        }
        else
        {
            ApplyState(PrepaidCardStep.CurrencySelection);
        }

        return Task.CompletedTask;
    }

    private Task SelectCurrencyAsync(string currencyCode, decimal currencyRate)
    {
        _selectedCurrencyCode = currencyCode;
        _selectedCurrencyRate = currencyRate;
        _selectedFundingType = string.Equals(currencyCode, "KRW", StringComparison.OrdinalIgnoreCase)
            ? PrepaidCardFundingType.BaseCash
            : PrepaidCardFundingType.ForeignCash;
        ApplyState(PrepaidCardStep.Consent);
        return Task.CompletedTask;
    }

    private Task ConfirmConsentAsync()
    {
        ApplyState(PrepaidCardStep.ScanIntro);
        return Task.CompletedTask;
    }

    private async Task RunScanAsync()
    {
        ApplyState(PrepaidCardStep.Scanning);
        _scanResult = await _scanSession.ExecuteAsync(TimeSpan.FromSeconds(20));
        ApplyState(PrepaidCardStep.ScanCompleted);
    }

    private async Task GoBackAsync()
    {
        switch (CurrentStep)
        {
            case PrepaidCardStep.Guide:
                if (_entrySource == PrepaidCardEntrySource.ExchangeEntry)
                    EntryBackRequested?.Invoke(this, EventArgs.Empty);
                else
                    HomeRequested?.Invoke(this, EventArgs.Empty);
                break;
            case PrepaidCardStep.ServiceSelection:
                ApplyState(PrepaidCardStep.Guide);
                break;
            case PrepaidCardStep.ChargeMethodSelection:
                ApplyState(PrepaidCardStep.ServiceSelection);
                break;
            case PrepaidCardStep.WalletSelection:
                ApplyState(PrepaidCardStep.ServiceSelection);
                break;
            case PrepaidCardStep.CardRecognition:
                ApplyState(PrepaidCardStep.WalletSelection);
                break;
            case PrepaidCardStep.PaymentMethodSelection:
                ApplyState(_selectedService == PrepaidCardServiceKind.ChargeExistingCard
                    ? PrepaidCardStep.CardRecognition
                    : PrepaidCardStep.ChargeMethodSelection);
                break;
            case PrepaidCardStep.CurrencySelection:
                ApplyState(_selectedChargeMethod == PrepaidCardChargeMethod.TrafficWalletOnly
                    ? PrepaidCardStep.PaymentMethodSelection
                    : _selectedService == PrepaidCardServiceKind.ChargeExistingCard
                    ? PrepaidCardStep.CardRecognition
                    : PrepaidCardStep.ChargeMethodSelection);
                break;
            case PrepaidCardStep.Consent:
                ApplyState(PrepaidCardStep.CurrencySelection);
                break;
            case PrepaidCardStep.ScanIntro:
                ApplyState(PrepaidCardStep.Consent);
                break;
            case PrepaidCardStep.ScanCompleted:
                ApplyState(PrepaidCardStep.ScanIntro);
                break;
            case PrepaidCardStep.DepositGuide:
                ApplyState(PrepaidCardStep.ScanCompleted);
                break;
            case PrepaidCardStep.Deposit:
                await StopDepositIfRunningAsync();
                ApplyState(PrepaidCardStep.DepositGuide);
                break;
            case PrepaidCardStep.AmountEntry:
                if (_selectedFundingType == PrepaidCardFundingType.EasyPay)
                {
                    ApplyState(PrepaidCardStep.PaymentMethodSelection);
                }
                else
                {
                    await ShowDepositStepAsync();
                }
                break;
        }
    }

    private void ApplyState(PrepaidCardStep step)
    {
        DetachCurrentStepSubscriptions();
        CurrentStep = step;
        ShowStepHeader = step is PrepaidCardStep.Consent
            or PrepaidCardStep.CurrencySelection
            or PrepaidCardStep.ScanIntro
            or PrepaidCardStep.Scanning
            or PrepaidCardStep.ScanCompleted
            or PrepaidCardStep.DepositGuide
            or PrepaidCardStep.Deposit
            or PrepaidCardStep.AmountEntry;
        CollapseShellChrome = false;
        CurrentProgressStage = step switch
        {
            PrepaidCardStep.CurrencySelection => 1,
            PrepaidCardStep.Consent or PrepaidCardStep.ScanIntro or PrepaidCardStep.Scanning or PrepaidCardStep.ScanCompleted => 2,
            PrepaidCardStep.DepositGuide or PrepaidCardStep.Deposit or PrepaidCardStep.AmountEntry => 3,
            _ => 0
        };
        CurrentStepViewModel = CreateStepViewModel(step);
        ConfigureStepActions(step);
        AttachCurrentStepSubscriptions();
    }

    private ExchangeStepViewModelBase CreateStepViewModel(PrepaidCardStep step)
        => step switch
        {
            PrepaidCardStep.Guide => new PrepaidCardGuideStepViewModel(),
            PrepaidCardStep.ServiceSelection => new PrepaidCardServiceSelectionStepViewModel(
                new AsyncRelayCommand(() => SelectServiceAsync(PrepaidCardServiceKind.PurchaseAndCharge)),
                new AsyncRelayCommand(() => SelectServiceAsync(PrepaidCardServiceKind.ChargeExistingCard))),
            PrepaidCardStep.ChargeMethodSelection => new PrepaidCardChargeMethodSelectionStepViewModel(
                new AsyncRelayCommand(() => SelectChargeMethodAsync(PrepaidCardChargeMethod.BothWallets)),
                new AsyncRelayCommand(() => SelectChargeMethodAsync(PrepaidCardChargeMethod.TrafficWalletOnly))),
            PrepaidCardStep.WalletSelection => new PrepaidCardWalletSelectionStepViewModel(
                new AsyncRelayCommand(() => SelectChargeMethodAsync(PrepaidCardChargeMethod.PrepaidWalletOnly)),
                new AsyncRelayCommand(() => SelectChargeMethodAsync(PrepaidCardChargeMethod.TrafficWalletOnly))),
            PrepaidCardStep.CardRecognition => CreateCardRecognitionStepViewModel(),
            PrepaidCardStep.PaymentMethodSelection => _selectedService == PrepaidCardServiceKind.ChargeExistingCard
                ? new PrepaidCardPaymentMethodSelectionStepViewModel(
                    new AsyncRelayCommand(() => SelectTrafficPaymentMethodAsync(PrepaidCardFundingType.ForeignCash)),
                    new AsyncRelayCommand(() => SelectTrafficPaymentMethodAsync(PrepaidCardFundingType.EasyPay)),
                    new AsyncRelayCommand(() => SelectTrafficPaymentMethodAsync(PrepaidCardFundingType.EasyPay)))
                : new PrepaidCardEasyPayMethodSelectionStepViewModel(
                    new AsyncRelayCommand(() => SelectTrafficPaymentMethodAsync(PrepaidCardFundingType.EasyPay)),
                    new AsyncRelayCommand(() => SelectTrafficPaymentMethodAsync(PrepaidCardFundingType.EasyPay))),
            PrepaidCardStep.CurrencySelection => new CurrencySelectionStepViewModel(
                _optionProvider.CreateCurrencyOptions(SelectCurrencyAsync, includeKrw: true)),
            PrepaidCardStep.Consent => new ConsentStepViewModel(new AsyncRelayCommand(ShowTermsModalAsync)),
            PrepaidCardStep.ScanIntro => new ScanIntroStepViewModel(),
            PrepaidCardStep.Scanning => new ScanningStepViewModel(),
            PrepaidCardStep.ScanCompleted => CreateScanCompletedStepViewModel(),
            PrepaidCardStep.DepositGuide => new PrepaidCardDepositGuideStepViewModel(),
            PrepaidCardStep.Deposit => new DepositStepViewModel(
                _selectedCurrencyCode ?? "USD",
                "KRW",
                _approvedDepositAmount,
                _approvedExchangeAmount,
                _selectedCurrencyRate ?? 0m,
                _depositLimit,
                DepositInfoVariant.PrepaidCard,
                _selectedService,
                ShowPrepaidLimitInfoCommand),
            PrepaidCardStep.AmountEntry => CreateAmountEntryStepViewModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unsupported prepaid card step.")
        };

    private ExchangeStepViewModelBase CreateAmountEntryStepViewModel()
    {
        var availableChargeAmount = CalculateAvailableChargeAmount();

        return _selectedFundingType switch
        {
            PrepaidCardFundingType.BaseCash => new PrepaidCardBaseCashAmountEntryStepViewModel(
                ShowChargeAmountOverlayAsync,
                availableChargeAmount,
                _selectedService),
            PrepaidCardFundingType.EasyPay => new PrepaidCardEasyPayAmountEntryStepViewModel(),
            _ => new PrepaidCardAmountEntryStepViewModel(
                ShowChargeAmountOverlayAsync,
                availableChargeAmount,
                _selectedService)
        };
    }

    private ScanCompletedStepViewModel CreateScanCompletedStepViewModel()
        => new(
            _scanResult?.Success == true,
            _scanResult?.Ocr?.DocumentType,
            _scanResult?.Ocr?.Fields,
            _scanResult?.ErrorMessage);

    private PrepaidCardCardRecognitionStepViewModel CreateCardRecognitionStepViewModel()
        => _selectedChargeMethod == PrepaidCardChargeMethod.TrafficWalletOnly
            ? new PrepaidCardCardRecognitionStepViewModel(
                "교통선불카드를 투입해주세요",
                "pack://application:,,,/Assets/Image/img_ezl.png")
            : new PrepaidCardCardRecognitionStepViewModel(
                "선불카드를 투입해주세요",
                "pack://application:,,,/Assets/Image/img_card_guide.png");

    private void ConfigureStepActions(PrepaidCardStep step)
    {
        if (CurrentStepViewModel is null)
            return;

        CurrentStepViewModel.SecondaryCommand = new AsyncRelayCommand(GoBackAsync);
        CurrentStepViewModel.IsSecondaryEnabled = true;
        CurrentStepViewModel.PrimaryCommand = null;
        CurrentStepViewModel.IsPrimaryEnabled = true;

        switch (step)
        {
            case PrepaidCardStep.Guide:
                CurrentStepViewModel.PrimaryCommand = new AsyncRelayCommand(() =>
                {
                    ApplyState(PrepaidCardStep.ServiceSelection);
                    return Task.CompletedTask;
                });
                break;
            case PrepaidCardStep.CardRecognition:
                CurrentStepViewModel.PrimaryCommand = new AsyncRelayCommand(ProceedFromCardRecognitionAsync);
                break;
            case PrepaidCardStep.Consent:
                CurrentStepViewModel.PrimaryCommand = new AsyncRelayCommand(ConfirmConsentAsync);
                CurrentStepViewModel.IsPrimaryEnabled = CurrentStepViewModel is ITermsAgreementStepViewModel { IsAgreed: true };
                break;
            case PrepaidCardStep.ScanIntro:
                CurrentStepViewModel.PrimaryCommand = new AsyncRelayCommand(RunScanAsync);
                CurrentStepViewModel.IsPrimaryEnabled = CurrentStepViewModel is IScanIntroStepViewModel scanIntro && scanIntro.CanProceed;
                break;
            case PrepaidCardStep.Scanning:
                CurrentStepViewModel.SecondaryCommand = null;
                break;
            case PrepaidCardStep.ScanCompleted:
                CurrentStepViewModel.SecondaryCommand = _scanResult?.Success == true
                    ? new AsyncRelayCommand(GoBackAsync)
                    : null;
                CurrentStepViewModel.PrimaryCommand = _scanResult?.Success == true
                    ? new AsyncRelayCommand(() =>
                    {
                        ApplyState(PrepaidCardStep.DepositGuide);
                        return Task.CompletedTask;
                    })
                    : new AsyncRelayCommand(() =>
                    {
                        ApplyState(PrepaidCardStep.ScanIntro);
                        return Task.CompletedTask;
                    });
                break;
            case PrepaidCardStep.DepositGuide:
                CurrentStepViewModel.PrimaryCommand = new AsyncRelayCommand(ShowDepositStepAsync);
                break;
            case PrepaidCardStep.Deposit:
                CurrentStepViewModel.PrimaryCommand = new AsyncRelayCommand(ProceedFromDepositAsync);
                break;
            case PrepaidCardStep.AmountEntry:
                CurrentStepViewModel.PrimaryCommand = new AsyncRelayCommand(() => Task.CompletedTask);
                break;
        }
    }

    private void AttachCurrentStepSubscriptions()
    {
        if (CurrentStepViewModel is ITermsAgreementStepViewModel termsVm)
            termsVm.PropertyChanged += OnTermsAgreementStepPropertyChanged;
        if (CurrentStepViewModel is IScanIntroStepViewModel scanIntroVm)
            scanIntroVm.PropertyChanged += OnScanIntroStepPropertyChanged;
    }

    private void DetachCurrentStepSubscriptions()
    {
        if (CurrentStepViewModel is ITermsAgreementStepViewModel termsVm)
            termsVm.PropertyChanged -= OnTermsAgreementStepPropertyChanged;
        if (CurrentStepViewModel is IScanIntroStepViewModel scanIntroVm)
            scanIntroVm.PropertyChanged -= OnScanIntroStepPropertyChanged;
    }

    private void OnTermsAgreementStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ITermsAgreementStepViewModel termsVm || e.PropertyName != nameof(ITermsAgreementStepViewModel.IsAgreed))
            return;

        if (CurrentStepViewModel is not null)
            CurrentStepViewModel.IsPrimaryEnabled = termsVm.IsAgreed;
    }

    private void OnScanIntroStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not IScanIntroStepViewModel scanIntroVm || e.PropertyName != nameof(IScanIntroStepViewModel.CanProceed))
            return;

        if (CurrentStepViewModel is not null)
            CurrentStepViewModel.IsPrimaryEnabled = scanIntroVm.CanProceed;
    }

    private void OnScanProgressChanged(object? sender, IdScannerEvent e)
    {
        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(() => OnScanProgressChanged(sender, e));
            return;
        }

        if (CurrentStepViewModel is IScannerEventConsumer scannerEventConsumer)
            scannerEventConsumer.ApplyScannerEvent(e);
    }

    private void OnDepositProgressChanged(object? sender, ExchangeDepositProgressChangedEventArgs e)
    {
        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(() => OnDepositProgressChanged(sender, e));
            return;
        }

        _approvedDepositAmount = e.ApprovedDepositAmount;
        _approvedExchangeAmount = e.ExchangedAmount;

        if (CurrentStepViewModel is IDepositProgressConsumer depositProgressConsumer)
            depositProgressConsumer.ApplyDepositProgress(e);
    }

    [RelayCommand]
    private void CloseModal()
    {
        CurrentModalViewModel = null;
    }

    [RelayCommand]
    private void ShowPrepaidLimitInfo()
    {
        if (CurrentStepViewModel is not DepositStepViewModel depositStepViewModel)
            return;

        CurrentModalViewModel = new PrepaidCardDepositLimitOverlayViewModel(
            depositStepViewModel.SourceCurrencyCode,
            depositStepViewModel.DailyMaximumAmountText,
            depositStepViewModel.DailyRemainingMaximumAmountText,
            CloseModalCommand);
    }

    private Task ShowTermsModalAsync()
    {
        CurrentModalViewModel = new TermsOverlayViewModel(CloseModalCommand);
        return Task.CompletedTask;
    }

    private async Task ShowDepositStepAsync()
    {
        await StopDepositIfRunningAsync();

        var sourceCurrency = string.IsNullOrWhiteSpace(_selectedCurrencyCode)
            ? "USD"
            : _selectedCurrencyCode.ToUpperInvariant();
        var exchangeRate = _selectedCurrencyRate ?? 0m;

        _depositLimit = await _depositLimitProvider.GetDepositLimitAsync(sourceCurrency);
        _approvedDepositAmount = 0m;
        _approvedExchangeAmount = 0m;

        ApplyState(PrepaidCardStep.Deposit);

        var start = await _depositSession.StartAsync(
            new ExchangeDepositSessionOptions(
                sourceCurrency,
                "KRW",
                exchangeRate,
                _depositLimit));

        if (!start.Success && CurrentStepViewModel is IDepositProgressConsumer depositProgressConsumer)
        {
            depositProgressConsumer.ApplyDepositProgress(
                new ExchangeDepositProgressChangedEventArgs(
                    _approvedDepositAmount,
                    _approvedExchangeAmount,
                    null,
                    sourceCurrency,
                    false,
                    start.ErrorMessage ?? "?낃툑湲곕? ?쒖옉?섏? 紐삵뻽?듬땲??"));
        }
    }

    private async Task ProceedFromDepositAsync()
    {
        await StopDepositIfRunningAsync();
        ApplyState(PrepaidCardStep.AmountEntry);
    }

    private async Task StopDepositIfRunningAsync()
    {
        await _depositSession.StopAsync();
    }

    private int CalculateAvailableChargeAmount()
    {
        var exchangedAmount = (int)decimal.Round(_approvedExchangeAmount, 0, MidpointRounding.AwayFromZero);
        var cardPurchaseAmount = _selectedService == PrepaidCardServiceKind.PurchaseAndCharge ? 5_000 : 0;
        return Math.Max(0, exchangedAmount - cardPurchaseAmount);
    }

    private Task ShowChargeAmountOverlayAsync(PrepaidCardWalletKind walletKind)
    {
        if (CurrentStepViewModel is not PrepaidCardAmountEntryStepViewModel amountEntryStep)
            return Task.CompletedTask;

        CurrentModalViewModel = new PrepaidCardChargeAmountOverlayViewModel(
            walletKind,
            amountEntryStep.GetWalletAmount(walletKind),
            amountEntryStep.AvailableChargeAmount,
            amountEntryStep.GetMaxChargeableAmount(walletKind),
            amount => amountEntryStep.SetWalletAmount(walletKind, amount),
            CloseModalCommand);

        return Task.CompletedTask;
    }
}
