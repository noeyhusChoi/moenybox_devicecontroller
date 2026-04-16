using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IdScannerTool.Services;
using IdScannerTool.ViewModels.Overlays;
using System.Reflection;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace IdScannerTool.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly LoadingViewModel _loading;
    private readonly SerialRegistrationViewModel _registration;
    private readonly MainRuntimeViewModel _main;
    private readonly IAppOverlayService _appOverlayService;
    private readonly IStartupSequenceService _startupSequenceService;
    private readonly IDeviceConnectionMonitorService _connectionMonitor;
    private readonly IAppUpdateService _appUpdateService;
    private UpdateOverlayViewModel? _updateOverlay;
    private bool _startupBusy;
    private bool _updateBusy;
    private bool _updateFlowVisible;
    private bool _overlayAwaitingConfirmation;
    private bool _customOverlayVisible;
    private TaskCompletionSource<bool>? _overlayConfirmTcs;
    private bool _suppressThemeApply;

    public ShellViewModel(
        LoadingViewModel loading,
        SerialRegistrationViewModel registration,
        MainRuntimeViewModel main,
        IAppOverlayService appOverlayService,
        IStartupSequenceService startupSequenceService,
        IDeviceConnectionMonitorService connectionMonitor,
        IAppUpdateService appUpdateService)
    {
        _loading = loading;
        _registration = registration;
        _main = main;
        _appOverlayService = appOverlayService;
        _startupSequenceService = startupSequenceService;
        _connectionMonitor = connectionMonitor;
        _appUpdateService = appUpdateService;

        _connectionMonitor.ConnectionFaulted += OnConnectionFaulted;
        _connectionMonitor.ConnectionRecovered += OnConnectionRecovered;
        _appOverlayService.OverlayChanged += OnAppOverlayChanged;

        _suppressThemeApply = true;
        var currentTheme = ApplicationThemeManager.GetAppTheme();
        IsDarkTheme = currentTheme == ApplicationTheme.Dark;
        _suppressThemeApply = false;

        CurrentViewModel = _loading;
        _ = InitializeStartupSequenceAsync();
    }

    [ObservableProperty]
    private object currentViewModel;

    [ObservableProperty]
    private bool isOverlayVisible;

    [ObservableProperty]
    private object? currentOverlayContent;

    [ObservableProperty]
    private bool isDarkTheme;

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (_suppressThemeApply)
        {
            return;
        }

        ApplicationThemeManager.Apply(
            value ? ApplicationTheme.Dark : ApplicationTheme.Light,
            WindowBackdropType.Mica,
            true);
    }

    private async Task InitializeStartupSequenceAsync()
    {
        if (_startupBusy)
        {
            return;
        }

        _startupBusy = true;
        try
        {
            ShowLoading();
            _loading.StartupStatusMessage = "로컬 시리얼키 확인 중...";
            _loading.StartupDetailMessage = "등록 상태를 확인합니다.";
            _loading.ResetRegisteredStartupStages();
            var startupResult = await Task.Run(
                () => _startupSequenceService.RunStartupAsync(ApplyStageProgressOnUi));
            await ApplyStartupSequenceResultAsync(startupResult);
        }
        catch (Exception ex)
        {
            _loading.StartupStatusMessage = "초기화 오류";
            _loading.StartupDetailMessage = ex.Message;
            _loading.SetRetryEnabled(true);
            ShowLoading();
        }
        finally
        {
            _startupBusy = false;
        }
    }

    private async Task<(bool Success, string? Serial, string Message)> ExtractForRegistrationAsync()
    {
        if (_startupBusy)
        {
            return (false, null, "초기화 작업이 진행 중입니다.");
        }

        var result = await _startupSequenceService.ExtractForRegistrationAsync();
        _loading.StartupStatusMessage = result.StartupStatusMessage;
        _loading.StartupDetailMessage = result.StartupDetailMessage;
        return (result.Success, result.ExtractedSerial, result.RegistrationMessage);
    }

    private async Task<(bool Success, string Message)> SaveRegistrationAsync(string serial)
    {
        var result = await _startupSequenceService.SaveRegistrationAsync(serial);
        _loading.StartupStatusMessage = result.StartupStatusMessage;
        _loading.StartupDetailMessage = result.StartupDetailMessage;
        return (result.Success, result.RegistrationMessage);
    }

    private async Task ApplyStartupSequenceResultAsync(StartupSequenceResult result)
    {
        _loading.StartupStatusMessage = result.StartupStatusMessage;
        _loading.StartupDetailMessage = result.StartupDetailMessage;
        _registration.SetState(
            registered: result.RegisteredSerial,
            extracted: result.ExtractedSerial,
            message: result.RegistrationMessage,
            canRegister: result.CanRegister);

        if (result.FinalState == StartupState.Ready && result.NextStep == StartupNextStep.ShowMain)
        {
            HideOverlay();
            await _main.RefreshCoreAsync();
            _main.LastResult = result.StartupDetailMessage;
            ShowMain();
            return;
        }

        if (result.FinalState == StartupState.Failed)
        {
            _loading.SetRetryEnabled(true);
            ShowLoading();
            return;
        }

        if (result.Transitions.Count > 0)
        {
            var lastTransition = result.Transitions[^1];
            _main.LastResult = $"StartupState: {lastTransition.From} -> {lastTransition.To} ({lastTransition.Reason})";
        }

        ShowRegistration();
    }

    private void ShowLoading()
    {
        _main.SetAutoStandbyEnabled(false);
        CurrentViewModel = _loading;
    }

    private void ShowRegistration()
    {
        _main.SetAutoStandbyEnabled(false);
        CurrentViewModel = _registration;
    }

    private void ShowMain()
    {
        CurrentViewModel = _main;
        _main.SetAutoStandbyEnabled(true);
    }

    [RelayCommand]
    private async Task OpenUpdatePopupAsync()
    {
        if (_updateBusy || _overlayAwaitingConfirmation)
        {
            return;
        }

        _updateOverlay = new UpdateOverlayViewModel(
            GetCurrentVersion(),
            updateAction: StartUpdateAsync,
            closeAction: CloseCustomOverlay);
        _updateOverlay.IsBusy = true;
        PauseMainBackgroundFlowsForUpdate();
        ShowCustomOverlay(_updateOverlay);

        try
        {
            var checkResult = await _appUpdateService.CheckForUpdatesAsync();
            ApplyUpdateCheckResult(checkResult);
        }
        catch (Exception ex)
        {
            if (_updateOverlay is not null)
            {
                _updateOverlay.IsBusy = false;
                _updateOverlay.LatestVersion = "-";
                _updateOverlay.CanUpdate = false;
                _updateOverlay.StatusMessage = ex.Message;
            }
        }
    }

    public static ShellViewModel Create(
        MainRuntimeViewModel main,
        IAppOverlayService appOverlayService,
        IStartupSequenceService startupSequenceService,
        IDeviceConnectionMonitorService connectionMonitor,
        IAppUpdateService appUpdateService)
    {
        var loading = new LoadingViewModel();
        ShellViewModel? shell = null;

        SerialRegistrationViewModel registration = new(
            extractFunc: () => shell!.ExtractForRegistrationAsync(),
            registerFunc: serial => shell!.SaveRegistrationAsync(serial),
            retryFunc: () => shell!.InitializeStartupSequenceAsync());

        shell = new ShellViewModel(
            loading,
            registration,
            main,
            appOverlayService,
            startupSequenceService,
            connectionMonitor,
            appUpdateService);
        loading.ConfigureActions(
            retryAction: () => shell.InitializeStartupSequenceAsync(),
            cancelAction: () => Application.Current?.Shutdown());
        return shell;
    }

    private void OnConnectionFaulted(object? sender, DeviceConnectionFaultEvent e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            _ = HandleConnectionFaultedAsync(e);
            return;
        }

        _ = dispatcher.InvokeAsync(async () => await HandleConnectionFaultedAsync(e));
    }

    private void OnConnectionRecovered(object? sender, DeviceConnectionRecoveredEvent e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            _ = HandleConnectionRecoveredAsync(e);
            return;
        }

        _ = dispatcher.InvokeAsync(async () => await HandleConnectionRecoveredAsync(e));
    }

    private void OnAppOverlayChanged(object? sender, AppOverlaySnapshot e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            ApplyAppOverlay(e);
            return;
        }

        _ = dispatcher.InvokeAsync(() => ApplyAppOverlay(e));
    }

    private void ApplyAppOverlay(AppOverlaySnapshot snapshot)
    {
        if (_customOverlayVisible)
        {
            if (!snapshot.IsVisible || !snapshot.ShowConfirmButton)
            {
                return;
            }
        }

        if (_overlayAwaitingConfirmation && snapshot.IsVisible && !snapshot.ShowConfirmButton)
        {
            return;
        }

        IsOverlayVisible = snapshot.IsVisible;
        if (!snapshot.IsVisible)
        {
            CurrentOverlayContent = null;
            return;
        }

        if (snapshot.ShowConfirmButton)
        {
            CurrentOverlayContent = new ConfirmOverlayViewModel(
                snapshot.Title,
                snapshot.Message,
                onConfirm: () => _overlayConfirmTcs?.TrySetResult(true));
            return;
        }

        CurrentOverlayContent = new ProgressOverlayViewModel(snapshot.Title, snapshot.Message, snapshot.IndicatorState);
    }

    private async Task HandleConnectionFaultedAsync(DeviceConnectionFaultEvent e)
    {
        if (!ReferenceEquals(CurrentViewModel, _main))
        {
            return;
        }

        if (_overlayAwaitingConfirmation || _updateFlowVisible)
        {
            return;
        }

        await _main.HandleConnectionLostAsync();
        _overlayAwaitingConfirmation = true;
        var confirmed = false;
        try
        {
            confirmed = await ShowOverlayWithConfirmationAsync(
                "장치 연결 오류",
                $"메시지: {e.Message}\n확인을 누르면 프로그램을 다시 시작합니다.");
        }
        finally
        {
            _overlayAwaitingConfirmation = false;
        }

        if (!confirmed)
        {
            return;
        }

        ShowLoading();
        _loading.StartupStatusMessage = "장치 연결 오류 감지";
        _loading.StartupDetailMessage = "프로그램을 다시 시작합니다.";
        _loading.ResetRegisteredStartupStages();
        await InitializeStartupSequenceAsync();
    }

    private async Task HandleConnectionRecoveredAsync(DeviceConnectionRecoveredEvent _)
    {
        await Task.CompletedTask;
    }

    private async Task<bool> ShowOverlayWithConfirmationAsync(string title, string message)
    {
        _overlayConfirmTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _appOverlayService.ShowConfirmation(title, message);
        var confirmed = await _overlayConfirmTcs.Task;
        HideOverlay();
        return confirmed;
    }

    private void HideOverlay()
    {
        _customOverlayVisible = false;
        ResumeMainBackgroundFlowsAfterUpdate();
        _overlayConfirmTcs?.TrySetResult(false);
        _overlayConfirmTcs = null;
        _appOverlayService.Hide();
    }

    private void ShowCustomOverlay(object overlayContent)
    {
        _customOverlayVisible = true;
        IsOverlayVisible = true;
        CurrentOverlayContent = overlayContent;
    }

    private void CloseCustomOverlay()
    {
        _customOverlayVisible = false;
        ResumeMainBackgroundFlowsAfterUpdate();
        if (!_overlayAwaitingConfirmation)
        {
            IsOverlayVisible = false;
            CurrentOverlayContent = null;
        }
    }

    private void ApplyUpdateCheckResult(AppUpdateCheckResult result)
    {
        if (_updateOverlay is null)
        {
            return;
        }

        _updateOverlay.IsBusy = false;
        _updateOverlay.StatusMessage = result.Message;
        _updateOverlay.CanUpdate = result.IsConfigured && result.IsUpdateAvailable && result.Update is not null;
        _updateOverlay.LatestVersion = result.Update?.Version ?? _updateOverlay.CurrentVersion;
    }

    #if false
    private async Task StartUpdateAsync()
    {
        if (_updateBusy || _updateOverlay is null)
        {
            return;
        }

        var checkResult = await _appUpdateService.CheckForUpdatesAsync();
        ApplyUpdateCheckResult(checkResult);
        if (!checkResult.IsConfigured || !checkResult.IsUpdateAvailable || checkResult.Update is null)
        {
            return;
        }

        _updateBusy = true;
        _updateOverlay.IsBusy = true;
        _updateOverlay.ShowProgress = true;
        _updateOverlay.ProgressPercent = 0;
        try
        {
            _updateOverlay.StatusMessage = $"버전 {checkResult.Update.Version} 다운로드 중입니다. 0%";
                "업데이트 다운로드",
                $"버전 {checkResult.Update.Version} 다운로드 중... 0%");

            await _appUpdateService.DownloadAndApplyAsync(
                checkResult.Update,
                progress => _appOverlayService.UpdateProgressMessage(
                    $"버전 {checkResult.Update.Version} 다운로드 중... {progress}%"));
        }
        catch (Exception ex)
        {
            _appOverlayService.Hide();
            _updateOverlay.IsBusy = false;
            _updateOverlay.StatusMessage = ex.Message;
            ShowCustomOverlay(_updateOverlay);
        }
        finally
        {
            _updateBusy = false;
        }
    }

    #endif

    private void PauseMainBackgroundFlowsForUpdate()
    {
        _updateFlowVisible = true;
        if (ReferenceEquals(CurrentViewModel, _main))
        {
            _main.SetUpdateFlowPaused(true);
        }
    }

    private void ResumeMainBackgroundFlowsAfterUpdate()
    {
        _updateFlowVisible = false;
        if (ReferenceEquals(CurrentViewModel, _main) && !_updateBusy)
        {
            _main.SetUpdateFlowPaused(false);
        }
    }

    private void ApplyStageProgressOnUi(StartupVerificationProgress progress)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            _loading.ApplyStageProgress(progress);
            return;
        }

        dispatcher.Invoke(() => _loading.ApplyStageProgress(progress));
    }

    private static string GetCurrentVersion()
    {
        var assembly = typeof(App).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?
            .Split('+')[0];
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
