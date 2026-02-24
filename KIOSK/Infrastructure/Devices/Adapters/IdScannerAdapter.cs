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
    public sealed class IdScannerAdapter : IIdScannerPort
    {
        private readonly IDeviceManager _deviceManager;
        private readonly object _detectedSync = new();
        private IIdScannerDriver? _detectedSource;

        public event EventHandler? Detected;

        public IdScannerAdapter(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task<Page?> SaveImageAsync(string deviceId, CancellationToken ct)
        {
            if (!_deviceManager.TryGetInnerDevice<IIdScannerDriver>(deviceId, out var driver))
                return null;

            var result = await driver.SaveImageAsync(ct).ConfigureAwait(false);
            return result?.Data as Page;
        }

        public async Task<CommandResult> ScanStartAsync(string deviceId, CancellationToken ct)
        {
            if (!_deviceManager.TryGetInnerDevice<IIdScannerDriver>(deviceId, out var driver))
                return CreateNotConnectedResult();

            EnsureDetectedBridge(deviceId);
            return await driver.StartScanAsync(ct).ConfigureAwait(false);
        }

        public async Task<CommandResult> GetScanStatusAsync(string deviceId, CancellationToken ct)
        {
            if (!_deviceManager.TryGetInnerDevice<IIdScannerDriver>(deviceId, out var driver))
                return CreateNotConnectedResult();

            return await driver.GetScanStatusAsync(ct).ConfigureAwait(false);
        }

        public async Task ScanStopAsync(string deviceId, CancellationToken ct)
        {
            if (!_deviceManager.TryGetInnerDevice<IIdScannerDriver>(deviceId, out var driver))
                return;

            await driver.StopScanAsync(ct).ConfigureAwait(false);
        }

        private void EnsureDetectedBridge(string deviceId)
        {
            if (!_deviceManager.TryGetInnerDevice<IIdScannerDriver>(deviceId, out var driver))
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

        private static CommandResult CreateNotConnectedResult()
            => new(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "COMMAND", "NOT_CONNECTED"));
    }
}
