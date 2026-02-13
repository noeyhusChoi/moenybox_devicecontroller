using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Infrastructure.Devices.Runtime;
using Pr22.Processing;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class IdScannerDeviceService : IIdScannerDeviceService
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IDeviceHost _deviceHost;
        private readonly object _detectedSync = new();
        private IdScannerDriver? _detectedSource;

        public event EventHandler? Detected;

        public IdScannerDeviceService(IDeviceManager deviceManager, IDeviceHost deviceHost)
        {
            _deviceManager = deviceManager;
            _deviceHost = deviceHost;
        }

        public async Task<Page?> SaveImageAsync(string deviceId, CancellationToken ct)
        {
            var result = await _deviceManager.SendAsync(deviceId, new DeviceCommand("SAVEIMAGE"), ct);
            return result?.Data as Page;
        }

        public Task<CommandResult> ScanStartAsync(string deviceId, CancellationToken ct)
        {
            EnsureDetectedBridge(deviceId);
            return _deviceManager.SendAsync(deviceId, new DeviceCommand("SCANSTART"), ct);
        }

        public Task<CommandResult> GetScanStatusAsync(string deviceId, CancellationToken ct) =>
            _deviceManager.SendAsync(deviceId, new DeviceCommand("GETSCANSTATUS"), ct);

        public Task ScanStopAsync(string deviceId, CancellationToken ct) =>
            _deviceManager.SendAsync(deviceId, new DeviceCommand("SCANSTOP"), ct);

        private void EnsureDetectedBridge(string deviceId)
        {
            if (!_deviceHost.TryGetSupervisor(deviceId, out var sup))
                return;

            var driver = sup.GetInnerDevice<IdScannerDriver>();
            if (driver is null)
                return;

            lock (_detectedSync)
            {
                if (ReferenceEquals(_detectedSource, driver))
                    return;

                if (_detectedSource is not null)
                    _detectedSource.Detected -= OnDriverDetected;

                driver.Detected += OnDriverDetected;
                _detectedSource = driver;
            }
        }

        private void OnDriverDetected(object? sender, EventArgs e)
            => Detected?.Invoke(this, EventArgs.Empty);
    }
}
