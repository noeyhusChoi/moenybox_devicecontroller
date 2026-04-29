using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class ExchangeStartStepViewModel : ExchangeStepViewModelBase
{
    public ExchangeStartStepViewModel(Func<Task>? startAction)
    {
        StartCommand = startAction is null ? null : new AsyncRelayCommand(startAction);
        ImageAssetPath = "pack://application:,,,/Assets/Image/Exchange.png";
    }

    public IAsyncRelayCommand? StartCommand { get; }
    public string? ImageAssetPath { get; }
    public bool ShowStartAction => StartCommand is not null;
}
