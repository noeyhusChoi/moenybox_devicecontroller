using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Management;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Status;
using KIOSK.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace KIOSK.Modules.Features.Environment.ViewModel;

public partial class DeviceStatusViewModel : ObservableObject, INavigable
{
    private readonly IStatusStore _statusStore;
    private readonly IDeviceHost _host;
    private readonly IDeviceCommandCatalog _commandCatalog;
    private readonly INavigationService _nav;

    public ObservableCollection<DeviceStatusItemViewModel> Devices { get; } = new();
    public ObservableCollection<DeviceCommandDescriptor> Commands { get; } = new();

    [ObservableProperty]
    private DeviceStatusItemViewModel? selectedDevice;

    public DeviceStatusViewModel(
        IStatusStore statusStore,
        IDeviceHost host,
        IDeviceCommandCatalog commandCatalog,
        INavigationService nav)
    {
        _statusStore = statusStore;
        _host = host;
        _commandCatalog = commandCatalog;
        _nav = nav;
    }

    public Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        LoadInitial();
        _statusStore.StatusUpdated += OnStatusUpdated;
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync()
    {
        _statusStore.StatusUpdated -= OnStatusUpdated;
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

    private void LoadInitial()
    {
        Devices.Clear();
        foreach (var sup in _host.GetAllSupervisors())
        {
            var snap = _statusStore.TryGet(sup.Name);
            var item = new DeviceStatusItemViewModel(sup);
            if (snap is not null)
                item.UpdateSnapshot(snap);
            Devices.Add(item);
        }

        SelectedDevice = Devices.FirstOrDefault();
    }

    private void OnStatusUpdated(string name, StatusSnapshot snapshot)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var existing = Devices.FirstOrDefault(d => d.Name == name);
            if (existing is not null)
            {
                existing.UpdateSnapshot(snapshot);
            }
            else if (_host.TryGetSupervisor(name, out var sup))
            {
                var item = new DeviceStatusItemViewModel(sup);
                item.UpdateSnapshot(snapshot);
                Devices.Add(item);
            }
        });
    }

    partial void OnSelectedDeviceChanged(DeviceStatusItemViewModel? value)
    {
        Commands.Clear();
        if (value is null)
            return;

        foreach (var cmd in _commandCatalog.GetFor(value.Name))
            Commands.Add(cmd);
    }
}

public sealed partial class DeviceStatusItemViewModel : ObservableObject
{
    public string Name { get; }
    public string Vendor { get; }
    public string Model { get; }
    public string DriverKey { get; }
    public string DeviceKey { get; }
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

    public DeviceStatusItemViewModel(DeviceSupervisor supervisor)
    {
        Name = supervisor.Name;
        Vendor = supervisor.Vendor;
        Model = supervisor.Model;
        DriverKey = supervisor.DriverKey;
        DeviceKey = supervisor.DeviceKey;
        TransportType = supervisor.TransportType;
        TransportPort = supervisor.TransportPort;
        TransportParam = supervisor.TransportParam;
        ProtocolName = supervisor.ProtocolName;
        PollingMs = supervisor.PollingMs;
    }

    public void UpdateSnapshot(StatusSnapshot snapshot)
    {
        Health = snapshot.Health;
        AlertScope = snapshot.AlertScope;
        Timestamp = snapshot.Timestamp;

        Alerts.Clear();
        if (snapshot.Alerts is not null)
        {
            foreach (var alert in snapshot.Alerts)
                Alerts.Add(alert);
        }

        OnPropertyChanged(nameof(AlertCount));
    }
}
