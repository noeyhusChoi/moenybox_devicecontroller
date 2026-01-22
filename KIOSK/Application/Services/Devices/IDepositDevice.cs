using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Devices
{
    public interface IDepositDevice
    {
        event EventHandler<string>? Escrowed;

        Task StartAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
        Task StackAsync(CancellationToken ct);
        Task ReturnAsync(CancellationToken ct);
    }
}
