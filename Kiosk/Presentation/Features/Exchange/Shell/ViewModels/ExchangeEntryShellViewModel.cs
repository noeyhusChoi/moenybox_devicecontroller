using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.Application.Services.Resx;
using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels;

public partial class ExchangeEntryShellViewModel : ObservableObject, IProgressChromeShellViewModel
{
    private readonly IAppCulture _appCulture;

    public event EventHandler? HomeRequested;
    public event EventHandler? CashExchangeRequested;
    public event EventHandler? PrepaidCardRequested;

    [ObservableProperty]
    private ExchangeStep currentStep;

    [ObservableProperty]
    private string timerText = "180";

    [ObservableProperty]
    private bool showStepHeader;

    [ObservableProperty]
    private bool collapseShellChrome;

    [ObservableProperty]
    private int currentProgressStage;

    [ObservableProperty]
    private object? currentStepViewModel;

    public ExchangeEntryShellViewModel(IAppCulture appCulture)
    {
        _appCulture = appCulture;
        ApplyState(ExchangeStep.LanguageSelection);
    }

    public Task StartFlowAsync()
    {
        ApplyState(ExchangeStep.LanguageSelection);
        return Task.CompletedTask;
    }

    public void ReturnToMethodSelection()
        => ApplyState(ExchangeStep.MethodSelection);

    private Task ConfirmStartAsync()
    {
        ApplyState(ExchangeStep.MethodSelection);
        return Task.CompletedTask;
    }

    private void SelectLanguage(HomeServiceType serviceType, string languageCode)
    {
        _appCulture.SetCulture(CultureInfo.GetCultureInfo(languageCode));
        ApplyState(ExchangeStep.Start);
    }

    private Task SelectMethodAsync(ExchangeMethod method)
    {
        if (method == ExchangeMethod.PrepaidCard)
            PrepaidCardRequested?.Invoke(this, EventArgs.Empty);
        else
            CashExchangeRequested?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    private Task GoBackAsync()
    {
        switch (CurrentStep)
        {
            case ExchangeStep.LanguageSelection:
                HomeRequested?.Invoke(this, EventArgs.Empty);
                break;
            case ExchangeStep.Start:
                ApplyState(ExchangeStep.LanguageSelection);
                break;
            case ExchangeStep.MethodSelection:
                ApplyState(ExchangeStep.Start);
                break;
            default:
                HomeRequested?.Invoke(this, EventArgs.Empty);
                break;
        }

        return Task.CompletedTask;
    }

    private void ApplyState(ExchangeStep step)
    {
        CurrentStep = step;
        ShowStepHeader = false;
        CollapseShellChrome = false;
        CurrentProgressStage = 0;
        CurrentStepViewModel = CreateStepViewModel(step);
        ConfigureStepActions(step);
    }

    private object CreateStepViewModel(ExchangeStep step)
        => step switch
        {
            ExchangeStep.LanguageSelection => new HomeLanguageSelectionViewModel(
                HomeServiceType.Exchange,
                GetSupportedLanguageCodes(),
                SelectLanguage),
            ExchangeStep.Start => new ExchangeStartStepViewModel(ConfirmStartAsync),
            ExchangeStep.MethodSelection => new MethodSelectionStepViewModel(
                new AsyncRelayCommand(() => SelectMethodAsync(ExchangeMethod.PrepaidCard)),
                new AsyncRelayCommand(() => SelectMethodAsync(ExchangeMethod.Cash))),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Unsupported exchange entry step.")
        };

    private void ConfigureStepActions(ExchangeStep step)
    {
        if (CurrentStepViewModel is not ExchangeStepViewModelBase stepViewModel)
            return;

        stepViewModel.SecondaryCommand = new AsyncRelayCommand(GoBackAsync);
        stepViewModel.IsSecondaryEnabled = true;

        if (step == ExchangeStep.Start)
            stepViewModel.SecondaryCommand = new AsyncRelayCommand(GoBackAsync);
    }

    private static IReadOnlyCollection<string> GetSupportedLanguageCodes()
        =>
        [
            "ko-KR",
            "en-US",
            "ja-JP",
            "zh-CN",
            "zh-TW"
        ];
}
