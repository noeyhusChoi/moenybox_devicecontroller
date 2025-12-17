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
            qr.Log += OnQrLog;
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
