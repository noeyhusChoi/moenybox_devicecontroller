using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.DeviceCommon.Devices;

public interface IDeviceRuntimePort
{
    Task<IReadOnlyList<DeviceStatusSnapshot>> GetStatusesAsync(CancellationToken cancellationToken = default);
    Task<DeviceStatusSnapshot?> GetStatusAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<DeviceCommandResult> ExecuteAsync(
        string deviceId,
        DeviceCommandRequest command,
        CancellationToken cancellationToken = default);
}
