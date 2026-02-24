using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers;

public interface IDepositDriver : IDeviceDriver
{
    event EventHandler<string>? Escrowed;

    Task<CommandResult> StartAcceptanceAsync(CancellationToken ct = default);
    Task<CommandResult> StopAcceptanceAsync(CancellationToken ct = default);
    Task<CommandResult> StackAsync(CancellationToken ct = default);
    Task<CommandResult> ReturnAsync(CancellationToken ct = default);
}
