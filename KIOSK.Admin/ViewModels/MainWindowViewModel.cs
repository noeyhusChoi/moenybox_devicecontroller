using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KIOSK.Admin.Messages;
using KIOSK.DeviceCommon.Devices;

namespace KIOSK.Admin.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient, IRecipient<DeviceStatusChangedMessage>
{
    private readonly IDeviceRuntimePort _runtimePort;

    public MainWindowViewModel(IDeviceRuntimePort runtimePort, IMessenger messenger) : base(messenger)
    {
        _runtimePort = runtimePort;
        IsActive = true;
        _ = RefreshAsync();
    }

    public ObservableCollection<DeviceStatusSnapshot> Devices { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    private DeviceStatusSnapshot? selectedDevice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    private string commandName = "RESTART";

    [ObservableProperty]
    private string commandPayload = string.Empty;

    [ObservableProperty]
    private string lastResult = string.Empty;

    [ObservableProperty]
    private string summaryText = "Ready";

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var statuses = await _runtimePort.GetStatusesAsync().ConfigureAwait(false);
        App.Current.Dispatcher.Invoke(() =>
        {
            Devices.Clear();
            foreach (var status in statuses)
                Devices.Add(status);
            SummaryText = $"Devices: {Devices.Count}";
        });
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        var deviceId = SelectedDevice?.DeviceId;
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        var changed = await _runtimePort.ConnectAsync(deviceId).ConfigureAwait(false);
        LastResult = changed ? $"Connected: {deviceId}" : $"No change: {deviceId}";
        await RefreshAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        var deviceId = SelectedDevice?.DeviceId;
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        var changed = await _runtimePort.DisconnectAsync(deviceId).ConfigureAwait(false);
        LastResult = changed ? $"Disconnected: {deviceId}" : $"No change: {deviceId}";
        await RefreshAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDeviceCommand))]
    private async Task ExecuteAsync()
    {
        var deviceId = SelectedDevice?.DeviceId;
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        var request = new DeviceCommandRequest(CommandName, string.IsNullOrWhiteSpace(CommandPayload) ? null : CommandPayload);
        var result = await _runtimePort.ExecuteAsync(deviceId, request).ConfigureAwait(false);
        LastResult = $"[{result.Code}] success={result.Success} message={result.Message}";
        await RefreshAsync().ConfigureAwait(false);
    }

    private bool CanConnect() => SelectedDevice is not null;
    private bool CanDisconnect() => SelectedDevice is not null;
    private bool CanExecuteDeviceCommand() => SelectedDevice is not null && !string.IsNullOrWhiteSpace(CommandName);

    public void Receive(DeviceStatusChangedMessage message)
    {
        var snapshot = message.Value;
        App.Current.Dispatcher.Invoke(() =>
        {
            var index = Devices
                .Select((item, i) => new { item, i })
                .FirstOrDefault(x => string.Equals(x.item.DeviceId, snapshot.DeviceId, StringComparison.OrdinalIgnoreCase))
                ?.i ?? -1;

            if (index >= 0)
                Devices[index] = snapshot;
            else
                Devices.Add(snapshot);

            SummaryText = $"Devices: {Devices.Count}";

            if (SelectedDevice is not null &&
                string.Equals(SelectedDevice.DeviceId, snapshot.DeviceId, StringComparison.OrdinalIgnoreCase))
            {
                SelectedDevice = snapshot;
            }
        });
    }
}
