using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using Pr22.Processing;

namespace KIOSK.Application.Services.Devices
{
    public interface IIdScannerDevice
    {
        event EventHandler? Detected;
        Task<Page?> SaveImageAsync(CancellationToken ct);
        Task<CommandResult> ScanStartAsync(CancellationToken ct);
        Task<CommandResult> GetScanStatusAsync(CancellationToken ct);
        Task ScanStopAsync(CancellationToken ct);
    }
}
