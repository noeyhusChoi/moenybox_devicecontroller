using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IdScannerTool.Services;
using System.Collections.Generic;

namespace IdScannerTool.ViewModels;

public partial class LoadingViewModel : ObservableObject
{
    private readonly Dictionary<StartupVerificationStage, StartupStageItemViewModel> _stageMap = new();
    private Func<Task>? _retryAction;
    private Action? _cancelAction;

    [ObservableProperty]
    private string startupStatusMessage = "시작 준비 중...";

    [ObservableProperty]
    private string startupDetailMessage = string.Empty;

    [ObservableProperty]
    private StartupStageItemViewModel? currentStage;

    [ObservableProperty]
    private bool isRetryEnabled;

    public IAsyncRelayCommand RetryCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public LoadingViewModel()
    {
        RetryCommand = new AsyncRelayCommand(ExecuteRetryAsync, CanRetry);
        CancelCommand = new RelayCommand(ExecuteCancel);
        ResetRegisteredStartupStages();
    }

    public void ConfigureActions(Func<Task> retryAction, Action cancelAction)
    {
        _retryAction = retryAction;
        _cancelAction = cancelAction;
    }

    public void ResetRegisteredStartupStages()
    {
        _stageMap.Clear();
        _stageMap[StartupVerificationStage.ConnectDevice] = new StartupStageItemViewModel(StartupVerificationStage.ConnectDevice, "장치 연결");
        _stageMap[StartupVerificationStage.ExtractSerial] = new StartupStageItemViewModel(StartupVerificationStage.ExtractSerial, "시리얼 추출");
        _stageMap[StartupVerificationStage.CompareSerial] = new StartupStageItemViewModel(StartupVerificationStage.CompareSerial, "시리얼 비교");

        CurrentStage = _stageMap[StartupVerificationStage.ConnectDevice];
        SetRetryEnabled(false);
    }

    public void SetRetryEnabled(bool enabled)
    {
        IsRetryEnabled = enabled;
    }

    public void ApplyStageProgress(StartupVerificationProgress progress)
    {
        if (!_stageMap.TryGetValue(progress.Stage, out var target))
        {
            return;
        }

        target.Message = progress.Message;
        target.Status = progress.Status;
        CurrentStage = target;
    }

    partial void OnIsRetryEnabledChanged(bool value)
    {
        RetryCommand.NotifyCanExecuteChanged();
    }

    private bool CanRetry() => IsRetryEnabled;

    private async Task ExecuteRetryAsync()
    {
        if (!CanRetry() || _retryAction is null)
        {
            return;
        }

        await _retryAction();
    }

    private void ExecuteCancel()
    {
        _cancelAction?.Invoke();
    }
}

public partial class StartupStageItemViewModel : ObservableObject
{
    public StartupStageItemViewModel(StartupVerificationStage stage, string title)
    {
        Stage = stage;
        Title = title;
    }

    public StartupVerificationStage Stage { get; }
    public string Title { get; }

    [ObservableProperty]
    private string message = "대기 중";

    [ObservableProperty]
    private StartupVerificationStageStatus status = StartupVerificationStageStatus.Pending;

    public string IconText => Status switch
    {
        StartupVerificationStageStatus.Running => "◌",
        StartupVerificationStageStatus.Succeeded => "✔",
        StartupVerificationStageStatus.Failed => "✕",
        _ => "○"
    };

    public bool IsSpinning => Status == StartupVerificationStageStatus.Running;
    public bool IsFailed => Status == StartupVerificationStageStatus.Failed;
    public bool IsRunning => Status == StartupVerificationStageStatus.Running;

    partial void OnStatusChanged(StartupVerificationStageStatus value)
    {
        OnPropertyChanged(nameof(IconText));
        OnPropertyChanged(nameof(IsSpinning));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(IsRunning));
    }
}
