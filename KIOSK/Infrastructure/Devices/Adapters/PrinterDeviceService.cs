using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class PrinterDeviceService : IPrinterDeviceService
    {
        private readonly IDeviceManager _deviceManager;

        public PrinterDeviceService(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task PrintTitleAsync(string deviceId, string content, CancellationToken ct = default)
        {
            await _deviceManager.SendAsync(deviceId, new DeviceCommand("PrintTitle", content), ct);
        }

        public async Task PrintContentAsync(string deviceId, string content, CancellationToken ct = default)
        {
            await _deviceManager.SendAsync(deviceId, new DeviceCommand("PrintContent", content), ct);
        }

        public async Task CutAsync(string deviceId, CancellationToken ct = default)
        {
            await _deviceManager.SendAsync(deviceId, new DeviceCommand("Cut"), ct);
        }
    }
}
