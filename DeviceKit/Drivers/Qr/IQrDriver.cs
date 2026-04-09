using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceKit.Drivers;

internal interface IQrDriver : IDeviceDriver
{
    Task<DeviceCommandResponse> EnableScanAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> DisableScanAsync(CancellationToken ct = default);
}
