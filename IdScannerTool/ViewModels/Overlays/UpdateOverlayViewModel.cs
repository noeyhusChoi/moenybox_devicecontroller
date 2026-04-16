using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IdScannerTool.ViewModels.Overlays;

public partial class UpdateOverlayViewModel : ObservableObject
{
    public UpdateOverlayViewModel(
        string currentVersion,
        Func<Task> updateAction,
        Action closeAction,
        Action cancelAction,
        Action restartAction)
    {
        CurrentVersion = currentVersion;
        LatestVersion = "-";
        StatusMessage = "최신 버전을 확인하는 중입니다.";

        UpdateCommand = new AsyncRelayCommand(updateAction, () => CanUpdate && !IsBusy);
        CloseCommand = new RelayCommand(closeAction, () => CanClose);
        CancelCommand = new RelayCommand(cancelAction, () => CanCancel);
        RestartCommand = new RelayCommand(restartAction, () => CanRestart);

        CloseButtonText = "닫기";
        UpdateButtonText = "업데이트";
    }

    public IAsyncRelayCommand UpdateCommand { get; }
    public IRelayCommand CloseCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand RestartCommand { get; }

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

    [ObservableProperty]
    private bool canClose = true;

    [ObservableProperty]
    private bool canCancel;

    [ObservableProperty]
    private bool canRestart;

    [ObservableProperty]
    private string closeButtonText;

    [ObservableProperty]
    private string updateButtonText;

    [ObservableProperty]
    private int progressPercent;

    [ObservableProperty]
    private bool showProgress;

    partial void OnCanUpdateChanged(bool value)
    {
        UpdateCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanCloseChanged(bool value)
    {
        CloseCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanCancelChanged(bool value)
    {
        CancelCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanRestartChanged(bool value)
    {
        RestartCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        UpdateCommand.NotifyCanExecuteChanged();
    }
}
