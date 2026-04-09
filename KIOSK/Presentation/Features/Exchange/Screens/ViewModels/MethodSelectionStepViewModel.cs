using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class MethodSelectionStepViewModel : ExchangeStepViewModelBase
{
    public MethodSelectionStepViewModel(
        IAsyncRelayCommand selectPrepaidCardCommand,
        IAsyncRelayCommand selectCashCommand,
        string? title = "환전 방법을 선택해주세요",
        string? body = "환전 방법을 선택해주세요")
    {
        Title = title;
        Body = body;
        SelectPrepaidCardCommand = selectPrepaidCardCommand;
        SelectCashCommand = selectCashCommand;
    }

    public IAsyncRelayCommand SelectPrepaidCardCommand { get; }
    public IAsyncRelayCommand SelectCashCommand { get; }
}
