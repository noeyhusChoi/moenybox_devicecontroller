using KIOSK.Application.Services.Devices;
using KIOSK.Device.Drivers;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class QrScannerAdapter : IQrScannerPort
    {
        private readonly IDeviceManager _deviceManager;
        private readonly object _sync = new();
        private IQrDriver? _decodedSource;

        public event EventHandler<QrDecodedEventArgs>? Decoded;

        public QrScannerAdapter(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public async Task EnableAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_deviceManager.TryGetInnerDevice<IQrDriver>(deviceId, out var driver))
                return;

            await driver.EnableScanAsync(ct).ConfigureAwait(false);
            AttachDecoded(deviceId);
        }

        public async Task DisableAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_deviceManager.TryGetInnerDevice<IQrDriver>(deviceId, out var driver))
                return;

            await driver.DisableScanAsync(ct).ConfigureAwait(false);
            DetachDecoded();
        }

        private void AttachDecoded(string deviceId)
        {
            if (!_deviceManager.TryGetInnerDevice<IQrDriver>(deviceId, out var driver))
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

        private void OnDriverDecoded(object? sender, QrDecodedData msg)
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
