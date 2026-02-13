using KIOSK.Device.Abstractions;

namespace KIOSK.Infrastructure.Health;

public enum HealthSourceKind
{
    Device,
    Database,
    Network,
    Disk,
    System
}

public sealed record HealthSignal(
    HealthSourceKind SourceKind,
    string SourceId,
    StatusSnapshot Snapshot)
{
    public static HealthSignal FromDevice(string deviceId, StatusSnapshot snapshot)
        => new(HealthSourceKind.Device, deviceId, snapshot);
}
