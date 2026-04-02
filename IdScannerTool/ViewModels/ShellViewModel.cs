using CommunityToolkit.Mvvm.ComponentModel;
using IdScannerTool.Services;
using IdScannerTool.ViewModels.Overlays;
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
    private bool _startupBusy;
    private bool _overlayAwaitingConfirmation;
    private TaskCompletionSource<bool>? _overlayConfirmTcs;
    private bool _suppressThemeApply;

    public ShellViewModel(
        LoadingViewModel loading,
        SerialRegistrationViewModel registration,
        MainRuntimeViewModel main,
        IAppOverlayService appOverlayService,
        IStartupSequenceService startupSequenceService,
        IDeviceConnectionMonitorService connectionMonitor)
    {
        _loading = loading;
        _registration = registration;
        _main = main;
        _appOverlayService = appOverlayService;
        _startupSequenceService = startupSequenceService;
        _connectionMonitor = connectionMonitor;

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

    public static ShellViewModel Create(
        MainRuntimeViewModel main,
        IAppOverlayService appOverlayService,
        IStartupSequenceService startupSequenceService,
        IDeviceConnectionMonitorService connectionMonitor)
    {
        var loading = new LoadingViewModel();
        ShellViewModel? shell = null;

        SerialRegistrationViewModel registration = new(
            extractFunc: () => shell!.ExtractForRegistrationAsync(),
            registerFunc: serial => shell!.SaveRegistrationAsync(serial),
            retryFunc: () => shell!.InitializeStartupSequenceAsync());

        shell = new ShellViewModel(loading, registration, main, appOverlayService, startupSequenceService, connectionMonitor);
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
        // 확인 오버레이 대기 중에는 다른 표시(Progress/Result)로 덮어쓰지 않되,
        // Hide(IsVisible=false)는 반드시 통과시켜 닫힘이 막히지 않게 한다.
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

        if (_overlayAwaitingConfirmation)
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
        // Recovery 이벤트는 자동 재검증을 트리거하지 않는다.
        // 재진입은 사용자의 확인(HandleConnectionFaultedAsync) 경로에서만 수행한다.
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
        _overlayConfirmTcs?.TrySetResult(false);
        _overlayConfirmTcs = null;
        _appOverlayService.Hide();
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
}
