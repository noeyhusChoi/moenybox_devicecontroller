using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using Pr22.Processing;

namespace KIOSK.Application.Services.Devices
{
    public interface IIdScannerDeviceService
    {
        event EventHandler? Detected;
        Task<Page?> SaveImageAsync(string deviceId, CancellationToken ct);
        Task<CommandResult> ScanStartAsync(string deviceId, CancellationToken ct);
        Task<CommandResult> GetScanStatusAsync(string deviceId, CancellationToken ct);
        Task ScanStopAsync(string deviceId, CancellationToken ct);
    }
}
