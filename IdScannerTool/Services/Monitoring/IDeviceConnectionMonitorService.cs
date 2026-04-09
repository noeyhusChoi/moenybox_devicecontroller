
namespace IdScannerTool.Services;

public sealed record DeviceConnectionFaultEvent(
    string DeviceId,
    DeviceConnectionState ConnectionState,
    string Message,
    int ConsecutiveDisconnectCount,
    DateTimeOffset TimestampUtc);

public sealed record DeviceConnectionRecoveredEvent(
    string DeviceId,
    DeviceConnectionState ConnectionState,
    string Message,
    DateTimeOffset TimestampUtc);

public interface IDeviceConnectionMonitorService
{
    event EventHandler<DeviceConnectionFaultEvent>? ConnectionFaulted;
    event EventHandler<DeviceConnectionRecoveredEvent>? ConnectionRecovered;
}
