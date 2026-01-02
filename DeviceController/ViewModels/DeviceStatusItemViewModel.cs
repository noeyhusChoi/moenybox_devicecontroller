using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Device.Abstractions;

namespace DeviceController.ViewModels;

public sealed partial class DeviceStatusItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string model = string.Empty;

    [ObservableProperty]
    private DeviceHealth health;

    [ObservableProperty]
    private DateTimeOffset timestamp;

    [ObservableProperty]
    private int alertCount;

    [ObservableProperty]
    private string alertsText = string.Empty;

    public void UpdateFrom(StatusSnapshot snapshot)
    {
        Name = snapshot.Name ?? string.Empty;
        Model = snapshot.Model ?? string.Empty;
        Health = snapshot.Health;
        Timestamp = snapshot.Timestamp;

        var alerts = snapshot.Alerts ?? new();
        AlertCount = alerts.Count;
        AlertsText = alerts.Count == 0
            ? "알림 없음"
            : string.Join(Environment.NewLine, alerts.Select(a => $"{a.Severity} {a.Code}: {a.Message}"));
    }
}

