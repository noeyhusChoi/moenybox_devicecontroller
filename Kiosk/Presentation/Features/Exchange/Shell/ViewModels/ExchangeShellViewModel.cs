using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.Orchestration;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.Application.Services.Devices.IdScanner;
using Kiosk.ViewModels.Overlays;
using Kiosk.ViewModels.Steps;
using System.ComponentModel;

namespace Kiosk.ViewModels;

public partial class ExchangeShellViewModel : ObservableObject, IModalSourceViewModel
{
    private readonly IExchangeFlowCoordinator _coordinator;
    private readonly IExchangeScreenFactory _screenFactory;

    public event EventHandler? HomeRequested;
    public event EventHandler<ExchangeCompletedEventArgs>? ExchangeCompleted;

    [ObservableProperty]
    private ExchangeStep currentStep;

    [ObservableProperty]
    private string timerText = "180";

    [ObservableProperty]
    private bool showStepHeader;

    [ObservableProperty]
    private bool useFeatureBackground;

    [ObservableProperty]
    private bool collapseShellChrome;

    [ObservableProperty]
    private int currentProgressStage;

    [ObservableProperty]
    private ExchangeStepViewModelBase? currentStepViewModel;

    [ObservableProperty]
    private object? currentModalViewModel;

    public ExchangeShellViewModel(
        IExchangeFlowCoordinator coordinator,
        IExchangeScreenFactory screenFactory)
    {
        _coordinator = coordinator;
        _screenFactory = screenFactory;
        _coordinator.FlowChanged += OnFlowChanged;
        _coordinator.ScanProgressChanged += OnScanProgressChanged;
        _coordinator.DepositProgressChanged += OnDepositProgressChanged;
        _coordinator.ExchangeCompleted += OnExchangeCompleted;
        ApplyState(_coordinator.Context.CurrentStep, _coordinator.Context);
    }

    public Task StartFlowAsync()
        => _coordinator.StartAsync();

    [RelayCommand]
    private Task RequestHomeAsync()
    {
        HomeRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private void OnFlowChanged(object? sender, ExchangeFlowChangedEventArgs e)
    {
        if (CurrentStepViewModel is null || e.Step != CurrentStep)
        {
            ApplyState(e.Step, e.Context);
            return;
        }

        ApplyChromeState(e.Step, e.Context);
    }

    private void ApplyState(ExchangeStep step, ExchangeFlowContext context)
    {
        DetachCurrentStepSubscriptions();
        CurrentModalViewModel = null;

        CurrentStep = step;
        ShowStepHeader = _screenFactory.ShouldShowStepHeader(step);
        UseFeatureBackground = _screenFactory.ShouldUseFeatureBackground(step);
        CollapseShellChrome = _screenFactory.ShouldCollapseShellChrome(step);

        CurrentProgressStage = ShowStepHeader
            ? _screenFactory.GetProgressStage(step)
            : 0;
        CurrentStepViewModel = _screenFactory.CreateStepViewModel(step, context, ShowModalAsync);
        _screenFactory.ConfigureStepActions(step, context, CurrentStepViewModel, RequestHomeCommand);

        if (CurrentStepViewModel is ITermsAgreementStepViewModel termsVm)
            termsVm.PropertyChanged += OnTermsAgreementStepPropertyChanged;
        if (CurrentStepViewModel is IScanIntroStepViewModel scanIntroVm)
            scanIntroVm.PropertyChanged += OnScanIntroStepPropertyChanged;
    }

    private void ApplyChromeState(ExchangeStep step, ExchangeFlowContext context)
    {
        CurrentStep = step;
        ShowStepHeader = _screenFactory.ShouldShowStepHeader(step);
        UseFeatureBackground = _screenFactory.ShouldUseFeatureBackground(step);
        CollapseShellChrome = _screenFactory.ShouldCollapseShellChrome(step);
        CurrentProgressStage = ShowStepHeader
            ? _screenFactory.GetProgressStage(step)
            : 0;

        _screenFactory.ConfigureStepActions(step, context, CurrentStepViewModel, RequestHomeCommand);
    }

    private void OnScanProgressChanged(object? sender, IdScannerEvent e)
    {
        if (CurrentStepViewModel is not IScannerEventConsumer scannerEventConsumer)
            return;

        scannerEventConsumer.ApplyScannerEvent(e);
    }

    private void OnDepositProgressChanged(object? sender, Kiosk.Application.Features.ExchangeV2.Services.ExchangeDepositProgressChangedEventArgs e)
    {
        if (CurrentStepViewModel is not IDepositProgressConsumer depositProgressConsumer)
            return;

        depositProgressConsumer.ApplyDepositProgress(e);
    }

    private void OnExchangeCompleted(object? sender, ExchangeCompletedEventArgs e)
    {
        ExchangeCompleted?.Invoke(this, e);
    }

    private async void OnTermsAgreementStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ITermsAgreementStepViewModel termsVm || e.PropertyName != nameof(ITermsAgreementStepViewModel.IsAgreed))
            return;

        await _coordinator.SetTermsAgreementAsync(termsVm.IsAgreed);
        ApplyChromeState(CurrentStep, _coordinator.Context);
    }

    private void OnScanIntroStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not IScanIntroStepViewModel scanIntroVm || e.PropertyName != nameof(IScanIntroStepViewModel.CanProceed))
            return;

        if (CurrentStepViewModel is not null)
            CurrentStepViewModel.IsPrimaryEnabled = scanIntroVm.CanProceed;
    }

    private void DetachCurrentStepSubscriptions()
    {
        if (CurrentStepViewModel is ITermsAgreementStepViewModel termsVm)
            termsVm.PropertyChanged -= OnTermsAgreementStepPropertyChanged;
        if (CurrentStepViewModel is IScanIntroStepViewModel scanIntroVm)
            scanIntroVm.PropertyChanged -= OnScanIntroStepPropertyChanged;
    }

    [RelayCommand]
    private void CloseModal()
    {
        CurrentModalViewModel = null;
    }

    private Task ShowModalAsync()
    {
        CurrentModalViewModel = new TermsOverlayViewModel(CloseModalCommand);
        return Task.CompletedTask;
    }
}
