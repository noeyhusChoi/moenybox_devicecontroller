using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Drivers.E200Z;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class QrScannerDeviceService : IQrScannerDeviceService
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IDeviceHost _deviceHost;
        private readonly object _sync = new();
        private QrE200ZDriver? _decodedSource;

        public event EventHandler<QrDecodedEventArgs>? Decoded;

        public QrScannerDeviceService(IDeviceManager deviceManager, IDeviceHost deviceHost)
        {
            _deviceManager = deviceManager;
            _deviceHost = deviceHost;
        }

        public async Task EnableAsync(string deviceId, CancellationToken ct = default)
        {
            await _deviceManager.SendAsync(deviceId, new DeviceCommand("SCAN_ENABLE"), ct).ConfigureAwait(false);
            AttachDecoded(deviceId);
        }

        public async Task DisableAsync(string deviceId, CancellationToken ct = default)
        {
            await _deviceManager.SendAsync(deviceId, new DeviceCommand("SCAN_DISABLE"), ct).ConfigureAwait(false);
            DetachDecoded();
        }

        private void AttachDecoded(string deviceId)
        {
            if (!_deviceHost.TryGetSupervisor(deviceId, out var sup))
                return;

            var driver = sup.GetInnerDevice<QrE200ZDriver>();
            if (driver is null)
                return;

            lock (_sync)
            {
                if (ReferenceEquals(_decodedSource, driver))
                    return;

                if (_decodedSource is not null)
                    _decodedSource.Decoded -= OnDriverDecoded;

                _decodedSource = driver;
                _decodedSource.Decoded += OnDriverDecoded;
            }
        }

        private void DetachDecoded()
        {
            lock (_sync)
            {
                if (_decodedSource is null)
                    return;

                _decodedSource.Decoded -= OnDriverDecoded;
                _decodedSource = null;
            }
        }

        private void OnDriverDecoded(object? sender, DecodeMessage msg)
        {
            var args = new QrDecodedEventArgs
            {
                BarcodeType = msg.BarcodeType,
                Text = msg.Text
            };

            Decoded?.Invoke(this, args);
        }
    }
}
