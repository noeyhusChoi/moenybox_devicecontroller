using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceKit.Drivers;

internal interface IDepositDriver : IDeviceDriver
{
    Task<DeviceCommandResponse> StartAcceptanceAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> StopAcceptanceAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> StackAsync(CancellationToken ct = default);
    Task<DeviceCommandResponse> ReturnAsync(CancellationToken ct = default);
}
