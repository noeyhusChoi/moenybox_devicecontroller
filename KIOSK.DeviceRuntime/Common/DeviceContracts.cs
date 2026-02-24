using System;

namespace KIOSK.DeviceCommon.Devices;

public enum DeviceConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Faulted = 3,
}

public sealed record DeviceStatusSnapshot(
    string DeviceId,
    string DeviceType,
    DeviceConnectionState ConnectionState,
    bool IsHealthy,
    string Message,
    DateTimeOffset Timestamp);

public sealed record DeviceCommandRequest(string Name, string? Payload = null);

public sealed record DeviceCommandResult(
    bool Success,
    string Code,
    string Message,
    string? RawResponse = null);
