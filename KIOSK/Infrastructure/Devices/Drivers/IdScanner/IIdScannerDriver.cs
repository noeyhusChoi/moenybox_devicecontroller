using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers;

public interface IIdScannerDriver : IDevice
{
    event EventHandler? Detected;

    Task<CommandResult> StartScanAsync(CancellationToken ct = default);
    Task<CommandResult> StopScanAsync(CancellationToken ct = default);
    Task<CommandResult> GetScanStatusAsync(CancellationToken ct = default);
    Task<CommandResult> SaveImageAsync(CancellationToken ct = default);
}
