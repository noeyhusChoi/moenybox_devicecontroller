using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Drivers.E200Z;
using KIOSK.Devices.Management;

namespace DeviceController.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IDeviceManager _deviceManager;
    private readonly IDeviceCommandCatalog _commandCatalog;

    [ObservableProperty]
    private DeviceStatusItemViewModel? selectedDevice;

    partial void OnSelectedDeviceChanged(DeviceStatusItemViewModel? value) => LoadCommandsForSelection();

    [ObservableProperty]
    private DeviceCommandDescriptor? selectedCommand;

    [ObservableProperty]
    private string payload = string.Empty;

    [ObservableProperty]
    private string response = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<DeviceStatusItemViewModel> Devices { get; } = new();
    public ObservableCollection<DeviceCommandDescriptor> Commands { get; } = new();
    public ObservableCollection<string> AsyncEvents { get; } = new();

    public IAsyncRelayCommand SendCommand { get; }

    private DeviceQrE200Z? _subscribedQr;
    private DeviceIdScanner? _subscribedIdScanner;
    private DeviceDeposit? _subscribedDeposit;
    private object? _logSource;
    private System.Reflection.EventInfo? _logEventInfo;
    private Delegate? _logHandler;

    public MainWindowViewModel(IDeviceManager deviceManager, IDeviceCommandCatalog commandCatalog)
    {
        _deviceManager = deviceManager;
        _commandCatalog = commandCatalog;

        foreach (var snap in _deviceManager.GetLatestSnapshots().OrderBy(x => x.Name))
        {
            var vm = new DeviceStatusItemViewModel();
            vm.UpdateFrom(snap);
            Devices.Add(vm);
        }

        _deviceManager.StatusUpdated += OnStatusUpdated;

        SendCommand = new AsyncRelayCommand(ExecuteCommandAsync, () => !IsBusy);
    }

    private async Task ExecuteCommandAsync()
    {
        if (SelectedDevice is null || SelectedCommand is null)
        {
            Response = "장치와 명령을 선택하세요.";
            return;
        }

        try
        {
            IsBusy = true;
            SendCommand.NotifyCanExecuteChanged();
            Response = "전송 중...";

            object? payloadObj = string.IsNullOrWhiteSpace(Payload) ? null : Payload;
            var cmd = new DeviceCommand(SelectedCommand.Name, payloadObj);
            var result = await _deviceManager.SendAsync(SelectedDevice.Name, cmd);

            if (result.Data is not null)
            {
                Response = result.Success
                    ? $"성공: {JsonSerializer.Serialize(result.Data)}"
                    : $"실패: {result.Message}, Data={JsonSerializer.Serialize(result.Data)}";
            }
            else
            {
                Response = result.Success ? "성공" : $"실패: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            Response = $"예외: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            SendCommand.NotifyCanExecuteChanged();
        }
    }

    private void LoadCommandsForSelection()
    {
        EnsureAsyncListenersForSelection();

        Commands.Clear();
        if (SelectedDevice is null)
            return;

        foreach (var cmd in _commandCatalog.GetFor(SelectedDevice.Name))
            Commands.Add(cmd);

        SelectedCommand = Commands.FirstOrDefault();
    }

    private void OnStatusUpdated(string name, DeviceStatusSnapshot snapshot)
    {
        if (Application.Current?.Dispatcher == null)
            UpdateCollection(snapshot);
        else
            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    UpdateCollection(snapshot);
                }
                catch (Exception ex)
                {
                    try { AppendAsyncEvent($"[UI] StatusUpdated error: {ex.Message}"); } catch { }
                }
            });
    }

    private void UpdateCollection(DeviceStatusSnapshot snapshot)
    {
        var existing = Devices.FirstOrDefault(d => d.Name.Equals(snapshot.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.UpdateFrom(snapshot);
            if (SelectedDevice is not null &&
                SelectedDevice.Name.Equals(snapshot.Name, StringComparison.OrdinalIgnoreCase))
            {
                EnsureAsyncListenersForSelection();
            }
        }
        else
        {
            var vm = new DeviceStatusItemViewModel();
            vm.UpdateFrom(snapshot);
            Devices.Add(vm);
        }
    }

    private void EnsureAsyncListenersForSelection()
    {
        if (SelectedDevice is null)
            return;

        // 공용 Log 이벤트(dynamic subscribe)
        DetachLog();

        var device = _deviceManager.GetDevice<IDevice>(SelectedDevice.Name);
        if (device is not null)
            AttachLog(device);

        var qr = _deviceManager.GetDevice<DeviceQrE200Z>(SelectedDevice.Name);
        if (ReferenceEquals(qr, _subscribedQr))
            return;

        if (_subscribedQr is not null)
        {
            _subscribedQr.Decoded -= OnQrDecoded;
            _subscribedQr.Log -= OnQrLog;
            _subscribedQr = null;
        }

        if (qr is not null)
        {
            _subscribedQr = qr;
            qr.Decoded += OnQrDecoded;
            //qr.Log += OnQrLog;
        }

        var idScanner = _deviceManager.GetDevice<DeviceIdScanner>(SelectedDevice.Name);
        if (ReferenceEquals(idScanner, _subscribedIdScanner))
            return;

        if (_subscribedIdScanner is not null)
        {
            _subscribedIdScanner.Detected -= OnIdScannerDetected;
            _subscribedIdScanner = null;
        }

        if (idScanner is not null)
        {
            _subscribedIdScanner = idScanner;
            idScanner.Detected += OnIdScannerDetected;
        }

        var deposit = _deviceManager.GetDevice<DeviceDeposit>(SelectedDevice.Name);
        if (ReferenceEquals(deposit, _subscribedDeposit))
            return;

        if (_subscribedDeposit is not null)
        {
            _subscribedDeposit.OnEscrowed -= OnDepositEscrowed;
            _subscribedDeposit = null;
        }

        if (deposit is not null)
        {
            _subscribedDeposit = deposit;
            deposit.OnEscrowed += OnDepositEscrowed;
        }
    }

    private void OnQrDecoded(object? sender, DecodeMessage msg)
    {
        AppendAsyncEvent($"[{SelectedDevice?.Name}] DECODE: {msg.Text}");
    }

    private void OnQrLog(string message)
    {
        AppendAsyncEvent(message);
    }

    private async void OnIdScannerDetected(object? sender, EventArgs e)
    {
        AppendAsyncEvent($"[{SelectedDevice?.Name}] DETECTED");
        try
        {
            if (SelectedDevice is null)
                return;

            await _deviceManager.SendAsync(SelectedDevice.Name, new DeviceCommand("SCANSTOP")).ConfigureAwait(false);
            AppendAsyncEvent($"[{SelectedDevice.Name}] SCANSTOP");
        }
        catch (Exception ex)
        {
            AppendAsyncEvent($"[IDSCANNER] SCANSTOP error: {ex.Message}");
        }
    }

    private void OnDepositEscrowed(object? sender, string value)
    {
        AppendAsyncEvent($"[{SelectedDevice?.Name}] ESCROWED: {value}");
    }

    #region Log subscription (dynamic)

    private void AttachLog(object device)
    {
        try
        {
            var evt = device.GetType().GetEvent("Log");
            if (evt?.EventHandlerType is null)
                return;

            var handlerType = evt.EventHandlerType;
            var invoke = handlerType.GetMethod("Invoke");
            var parameters = invoke?.GetParameters();
            if (parameters is null)
                return;

            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                _logHandler = Delegate.CreateDelegate(handlerType, this, nameof(HandleLogSingle));
            }
            else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(string))
            {
                _logHandler = Delegate.CreateDelegate(handlerType, this, nameof(HandleLogDual));
            }
            else
            {
                return;
            }

            evt.AddEventHandler(device, _logHandler);
            _logSource = device;
            _logEventInfo = evt;
        }
        catch
        {
            DetachLog();
        }
    }

    private void DetachLog()
    {
        try
        {
            if (_logSource is not null && _logEventInfo is not null && _logHandler is not null)
            {
                _logEventInfo.RemoveEventHandler(_logSource, _logHandler);
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            _logSource = null;
            _logEventInfo = null;
            _logHandler = null;
        }
    }

    private void HandleLogSingle(string message) => AppendAsyncEvent(message);
    private void HandleLogDual(object? sender, string message) => AppendAsyncEvent(message);

    #endregion

    private void AppendAsyncEvent(string message)
    {
        if (Application.Current?.Dispatcher != null)
        {
            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AsyncEvents.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
                if (AsyncEvents.Count > 200)
                    AsyncEvents.RemoveAt(AsyncEvents.Count - 1);
            });
        }
        else
        {
            AsyncEvents.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
            if (AsyncEvents.Count > 200)
                AsyncEvents.RemoveAt(AsyncEvents.Count - 1);
        }
    }
}
