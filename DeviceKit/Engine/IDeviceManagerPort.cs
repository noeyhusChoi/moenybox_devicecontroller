using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceKit.Engine;

/// <summary>
/// Single public entry point for device management.
/// Runtime operations + status event + command listing are unified here.
/// </summary>
public interface IDeviceManagerPort
{
    event Action<StatusSnapshot> DeviceStatusObserved;
    event Action<DeviceConnectionSnapshot> ConnectionObserved;
    event Action<DeviceEventEnvelope> DeviceEventReceived;

    Task<IReadOnlyList<StatusSnapshot>> GetStatusesAsync(CancellationToken cancellationToken = default);
    Task<StatusSnapshot?> GetStatusAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceConnectionSnapshot>> GetConnectionsAsync(CancellationToken cancellationToken = default);
    Task<DeviceConnectionSnapshot?> GetConnectionAsync(string deviceId, CancellationToken cancellationToken = default);
    Task AddAsync(DeviceDescriptor descriptor, CancellationToken cancellationToken = default);
    bool TryGetDevice(string deviceId, out DeviceDescriptor info);
    IReadOnlyList<DeviceDescriptor> GetAllDevices();
    Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<DeviceCommandResponse> ExecuteAsync(
        string deviceId,
        DeviceCommandRequest command,
        CancellationToken cancellationToken = default);

    IReadOnlyCollection<string> GetCommands(string deviceId);
}
