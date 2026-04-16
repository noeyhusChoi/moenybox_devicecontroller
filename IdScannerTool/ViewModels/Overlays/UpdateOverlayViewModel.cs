using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IdScannerTool.ViewModels.Overlays;

public partial class UpdateOverlayViewModel : ObservableObject
{
    public UpdateOverlayViewModel(
        string currentVersion,
        Func<Task> updateAction,
        Action closeAction)
    {
        CurrentVersion = currentVersion;
        LatestVersion = "-";
        StatusMessage = "최신 버전을 확인하는 중입니다.";
        UpdateCommand = new AsyncRelayCommand(updateAction, () => CanUpdate && !IsBusy);
        CloseCommand = new RelayCommand(closeAction, () => !IsBusy);
    }

    public IAsyncRelayCommand UpdateCommand { get; }
    public IRelayCommand CloseCommand { get; }

    public string Title => "업데이트";

    [ObservableProperty]
    private string currentVersion;

    [ObservableProperty]
    private string latestVersion;

    [ObservableProperty]
    private string statusMessage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool canUpdate;

    partial void OnCanUpdateChanged(bool value)
    {
        UpdateCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        UpdateCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }
}
