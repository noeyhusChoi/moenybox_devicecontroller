using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class ExchangeStartStepViewModel : ExchangeStepViewModelBase
{
    public ExchangeStartStepViewModel(Func<Task>? startAction)
    {
        Title = "외화를 원화로 환전할 수 있습니다.";
        Body = "외화를 원화로 환전할 수 있습니다.";
        StartText = "외화 판매 시작하기";
        StartCommand = startAction is null ? null : new AsyncRelayCommand(startAction);
        ImageAssetPath = "pack://application:,,,/Assets/Image/Exchange.png";
    }

    public string? StartText { get; }
    public IAsyncRelayCommand? StartCommand { get; }
    public string? ImageAssetPath { get; }
    public bool ShowStartAction => StartCommand is not null;
}
