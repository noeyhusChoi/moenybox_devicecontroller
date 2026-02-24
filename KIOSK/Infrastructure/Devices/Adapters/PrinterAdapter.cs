using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Drivers;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class PrinterAdapter : IPrinterPort
    {
        private readonly IDeviceManager _deviceManager;

        public PrinterAdapter(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task PrintTitleAsync(string deviceId, string content, CancellationToken ct = default)
        {
            if (!_deviceManager.TryGetInnerDevice<IPrinterDriver>(deviceId, out var driver))
                return;

            await driver.PrintTitleAsync(content, ct).ConfigureAwait(false);
        }

        public async Task PrintContentAsync(string deviceId, string content, CancellationToken ct = default)
        {
            if (!_deviceManager.TryGetInnerDevice<IPrinterDriver>(deviceId, out var driver))
                return;

            await driver.PrintContentAsync(content, ct).ConfigureAwait(false);
        }

        public async Task CutAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_deviceManager.TryGetInnerDevice<IPrinterDriver>(deviceId, out var driver))
                return;

            await driver.CutAsync(ct).ConfigureAwait(false);
        }
    }
}
