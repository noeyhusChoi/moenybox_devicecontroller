using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public sealed class ExchangeStartStepViewModel : ExchangeStepViewModelBase
{
    public ExchangeStartStepViewModel(Func<Task>? primaryAction)
    {
        Title = "외화를 원화로 환전이 가능합니다.";
        Body = "외화를 원화로 환전이 가능합니다.";
        PrimaryText = "외화 판매 시작하기";
        PrimaryCommand = primaryAction is null ? null : new AsyncRelayCommand(primaryAction);
        ImageAssetPath = "pack://application:,,,/Assets/Image/Exchange.png";
    }

    public string? PrimaryText { get; }
    public IAsyncRelayCommand? PrimaryCommand { get; }
    public string? ImageAssetPath { get; }
    public bool ShowPrimaryAction => PrimaryCommand is not null;
}
