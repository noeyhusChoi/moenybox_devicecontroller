using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class MethodSelectionStepViewModel : ExchangeStepViewModelBase
{
    public MethodSelectionStepViewModel(
        IAsyncRelayCommand selectPrepaidCardCommand,
        IAsyncRelayCommand selectCashCommand)
    {
        SelectPrepaidCardCommand = selectPrepaidCardCommand;
        SelectCashCommand = selectCashCommand;
    }

    public IAsyncRelayCommand SelectPrepaidCardCommand { get; }
    public IAsyncRelayCommand SelectCashCommand { get; }
}
