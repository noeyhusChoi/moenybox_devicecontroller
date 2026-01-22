using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Management.Devices;
using Pr22.Processing;

namespace KIOSK.Application.Services.Devices
{
    public sealed class IdScannerDeviceService : IIdScannerDeviceService
    {
        private readonly IDeviceManager _deviceManager;

        public IdScannerDeviceService(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task<Page?> SaveImageAsync(string deviceId, CancellationToken ct)
        {
            var result = await _deviceManager.SendAsync(deviceId, new DeviceCommand("SAVEIMAGE"), ct);
            return result?.Data as Page;
        }

        public Task<CommandResult> ScanStartAsync(string deviceId, CancellationToken ct) =>
            _deviceManager.SendAsync(deviceId, new DeviceCommand("SCANSTART"), ct);

        public Task<CommandResult> GetScanStatusAsync(string deviceId, CancellationToken ct) =>
            _deviceManager.SendAsync(deviceId, new DeviceCommand("GETSCANSTATUS"), ct);

        public Task ScanStopAsync(string deviceId, CancellationToken ct) =>
            _deviceManager.SendAsync(deviceId, new DeviceCommand("SCANSTOP"), ct);
    }
}
