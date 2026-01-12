using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Application.Services.Devices;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using WpfApplication = System.Windows.Application;

namespace KIOSK.Presentation.Features.Environment.ViewModels;

public partial class DeviceStatusViewModel : ObservableObject, INavigable
{
    private readonly IDeviceStatusService _statusService;
    private readonly IDeviceCommandCatalogService _commandCatalog;
    private readonly IDeviceManager _deviceManager;
    private readonly INavigationService _nav;
    private SynchronizationContext? _uiContext;

    public ObservableCollection<DeviceStatusItemViewModel> Devices { get; } = new();
    public ObservableCollection<DeviceCommandInfo> Commands { get; } = new();

    [ObservableProperty]
    private DeviceStatusItemViewModel? selectedDevice;

    [ObservableProperty]
    private DeviceCommandInfo? selectedCommand;

    [ObservableProperty]
    private bool isSending;

    public DeviceStatusViewModel(
        IDeviceStatusService statusService,
        IDeviceCommandCatalogService commandCatalog,
        IDeviceManager deviceManager,
        INavigationService nav)
    {
        _statusService = statusService;
        _commandCatalog = commandCatalog;
        _deviceManager = deviceManager;
        _nav = nav;
    }

    public Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        LoadInitial();
        _uiContext = SynchronizationContext.Current;
        _statusService.StatusUpdated += OnStatusUpdated;
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync()
    {
        _statusService.StatusUpdated -= OnStatusUpdated;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Back()
    {
        await _nav.NavigateTo<KIOSK.ViewModels.EnvironmentViewModel>();
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadInitial();
    }

    private bool CanSendCommand()
        => !IsSending && SelectedDevice is not null && SelectedCommand is not null;

    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private async Task SendSelectedCommandAsync()
    {
        if (SelectedDevice is null || SelectedCommand is null)
            return;

        IsSending = true;
        try
        {
            var command = new DeviceCommand(SelectedCommand.Name);
            await _deviceManager.SendAsync(
                SelectedDevice.Name,
                command,
                CommandContext.Manual(reason: "DeviceStatusView"));
        }
        finally
        {
            IsSending = false;
        }
    }

    private void LoadInitial()
    {
        Devices.Clear();
        foreach (var device in _statusService.GetDevices())
        {
            var snap = _statusService.TryGet(device.Name);
            var item = new DeviceStatusItemViewModel(device);
            if (snap is not null)
                item.UpdateSnapshot(snap);
            Devices.Add(item);
        }

        SelectedDevice = Devices.FirstOrDefault();
    }

    private void OnStatusUpdated(string name, StatusSnapshot snapshot)
    {
        void ApplyUpdate()
        {
            var existing = Devices.FirstOrDefault(d => d.Name == name);
            if (existing is not null)
            {
                existing.UpdateSnapshot(snapshot);
            }
            else if (_statusService.TryGetDevice(name, out var device))
            {
                var item = new DeviceStatusItemViewModel(device);
                item.UpdateSnapshot(snapshot);
                Devices.Add(item);
            }
        }

        if (_uiContext is null)
            ApplyUpdate();
        else
            _uiContext.Post(_ => ApplyUpdate(), null);
    }

    partial void OnSelectedDeviceChanged(DeviceStatusItemViewModel? value)
    {
        Commands.Clear();
        SelectedCommand = null;
        if (value is null)
            return;

        foreach (var cmd in _commandCatalog.GetFor(value.Name))
            Commands.Add(cmd);

        NotifySendCanExecuteChanged();
    }

    partial void OnSelectedCommandChanged(DeviceCommandInfo? value)
        => NotifySendCanExecuteChanged();

    partial void OnIsSendingChanged(bool value)
        => NotifySendCanExecuteChanged();

    private void NotifySendCanExecuteChanged()
    {
        var dispatcher = WpfApplication.Current?.Dispatcher;
        if (dispatcher is not null)
        {
            if (dispatcher.CheckAccess())
            {
                SendSelectedCommandCommand.NotifyCanExecuteChanged();
                return;
            }

            dispatcher.BeginInvoke(new Action(() =>
                SendSelectedCommandCommand.NotifyCanExecuteChanged()));
            return;
        }

        if (_uiContext is null)
            SendSelectedCommandCommand.NotifyCanExecuteChanged();
        else
            _uiContext.Post(_ => SendSelectedCommandCommand.NotifyCanExecuteChanged(), null);
    }
}

public sealed partial class DeviceStatusItemViewModel : ObservableObject
{
    public string Name { get; }
    public string Vendor { get; }
    public string Model { get; }
    public string TransportType { get; }
    public string TransportPort { get; }
    public string TransportParam { get; }
    public string ProtocolName { get; }
    public int PollingMs { get; }

    [ObservableProperty]
    private DeviceHealth health;

    [ObservableProperty]
    private AlertSource alertScope;

    [ObservableProperty]
    private DateTimeOffset timestamp;

    [ObservableProperty]
    private ObservableCollection<StatusEvent> alerts = new();

    public int AlertCount => Alerts.Count;

    public DeviceStatusItemViewModel(DeviceStatusInfo device)
    {
        Name = device.Name;
        Vendor = device.Vendor;
        Model = device.Model;
        TransportType = device.TransportType;
        TransportPort = device.TransportPort;
        TransportParam = device.TransportParam;
        ProtocolName = device.ProtocolName;
        PollingMs = device.PollingMs;
    }

    public void UpdateSnapshot(StatusSnapshot snapshot)
    {
        Health = snapshot.Health;
        AlertScope = snapshot.AlertScope;
        Timestamp = snapshot.Timestamp;

        if (!AreSameAlerts(Alerts, snapshot.Alerts))
        {
            Alerts.Clear();
            if (snapshot.Alerts is not null)
            {
                foreach (var alert in snapshot.Alerts)
                    Alerts.Add(alert);
            }

            OnPropertyChanged(nameof(AlertCount));
        }
    }

    private static bool AreSameAlerts(
        ObservableCollection<StatusEvent> current,
        IReadOnlyCollection<StatusEvent>? next)
    {
        if (next is null)
            return current.Count == 0;

        if (current.Count != next.Count)
            return false;

        static string Key(StatusEvent e)
            => $"{e.Source}:{e.ErrorCode?.ToString() ?? e.Code ?? string.Empty}";

        var currentKeys = current.Select(Key)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nextKeys = next.Select(Key)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return currentKeys.SetEquals(nextKeys);
    }
}
