using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceKit.Drivers;

internal interface IIdScannerDriver : IDeviceDriver
{
    Task<DeviceCommandResponse> StartScanAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> StopScanAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> GetScanStatusAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> SaveImageAsync(CancellationToken ct = default);
}
