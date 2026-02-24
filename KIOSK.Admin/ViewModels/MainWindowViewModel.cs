using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KIOSK.Admin.Messages;
using KIOSK.Admin.Services;
using KIOSK.DeviceCommon.Devices;

namespace KIOSK.Admin.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient, IRecipient<DeviceStatusChangedMessage>
{
    private readonly IDeviceRuntimePort _runtimePort;
    private readonly IAdminDeviceCommandCatalog _commandCatalog;

    public MainWindowViewModel(
        IDeviceRuntimePort runtimePort,
        IAdminDeviceCommandCatalog commandCatalog,
        IMessenger messenger) : base(messenger)
    {
        _runtimePort = runtimePort;
        _commandCatalog = commandCatalog;
        IsActive = true;
        _ = RefreshAsync();
    }

    public ObservableCollection<DeviceStatusItemViewModel> Devices { get; } = new();
    public ObservableCollection<AdminDeviceCommandItem> AvailableCommands { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    private DeviceStatusItemViewModel? selectedDevice;

    partial void OnSelectedDeviceChanged(DeviceStatusItemViewModel? value)
    {
        RefreshAvailableCommands(value?.DeviceId, value?.DeviceType);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    private string selectedCommandName = string.Empty;

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
            var indexById = Devices
                .Select((item, index) => new { item.DeviceId, index })
                .ToDictionary(x => x.DeviceId, x => x.index, StringComparer.OrdinalIgnoreCase);

            var incomingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var status in statuses)
            {
                incomingIds.Add(status.DeviceId);
                if (indexById.TryGetValue(status.DeviceId, out var idx))
                {
                    Devices[idx].Update(status);
                }
                else
                {
                    Devices.Add(new DeviceStatusItemViewModel(status));
                }
            }

            for (var i = Devices.Count - 1; i >= 0; i--)
            {
                if (!incomingIds.Contains(Devices[i].DeviceId))
                    Devices.RemoveAt(i);
            }

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

        var request = new DeviceCommandRequest(
            SelectedCommandName,
            string.IsNullOrWhiteSpace(CommandPayload) ? null : CommandPayload);
        var result = await _runtimePort.ExecuteAsync(deviceId, request).ConfigureAwait(false);
        LastResult = $"[{result.Code}] success={result.Success} message={result.Message}";
        await RefreshAsync().ConfigureAwait(false);
    }

    private bool CanConnect() => SelectedDevice is not null;
    private bool CanDisconnect() => SelectedDevice is not null;
    private bool CanExecuteDeviceCommand() => SelectedDevice is not null && !string.IsNullOrWhiteSpace(SelectedCommandName);

    public void Receive(DeviceStatusChangedMessage message)
    {
        var snapshot = message.Value;
        App.Current.Dispatcher.Invoke(() =>
        {
            var existing = Devices.FirstOrDefault(x =>
                string.Equals(x.DeviceId, snapshot.DeviceId, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Update(snapshot);
            }
            else
            {
                Devices.Add(new DeviceStatusItemViewModel(snapshot));
            }

            SummaryText = $"Devices: {Devices.Count}";
        });
    }

    private void RefreshAvailableCommands(string? deviceId, string? deviceType)
    {
        var commands = _commandCatalog.GetByDevice(deviceId, deviceType).ToList();

        if (AreSameCommands(AvailableCommands, commands))
            return;

        AvailableCommands.Clear();
        foreach (var item in commands)
            AvailableCommands.Add(item);

        if (AvailableCommands.Count == 0)
        {
            SelectedCommandName = string.Empty;
            return;
        }

        if (AvailableCommands.All(x => !string.Equals(x.Name, SelectedCommandName, StringComparison.OrdinalIgnoreCase)))
            SelectedCommandName = AvailableCommands[0].Name;
    }

    private static bool AreSameCommands(
        IReadOnlyList<AdminDeviceCommandItem> current,
        IReadOnlyList<AdminDeviceCommandItem> next)
    {
        if (current.Count != next.Count)
            return false;

        for (var i = 0; i < current.Count; i++)
        {
            var left = current[i];
            var right = next[i];
            if (!string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(left.Description, right.Description, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}

public partial class DeviceStatusItemViewModel : ObservableObject
{
    public DeviceStatusItemViewModel(DeviceStatusSnapshot snapshot)
    {
        DeviceId = snapshot.DeviceId;
        Update(snapshot);
    }

    public string DeviceId { get; }

    [ObservableProperty]
    private string deviceType = string.Empty;

    [ObservableProperty]
    private DeviceConnectionState connectionState;

    [ObservableProperty]
    private bool isHealthy;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private DateTimeOffset timestamp;

    public void Update(DeviceStatusSnapshot snapshot)
    {
        DeviceType = snapshot.DeviceType;
        ConnectionState = snapshot.ConnectionState;
        IsHealthy = snapshot.IsHealthy;
        Message = snapshot.Message;
        Timestamp = snapshot.Timestamp;
    }
}
