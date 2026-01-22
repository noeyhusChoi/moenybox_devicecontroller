using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Devices
{
    public sealed class DepositDevice : IDepositDevice
    {
        private const string DeviceId = "DEPOSIT1";
        private readonly IDepositDeviceService _service;
        private bool _subscribed;

        public DepositDevice(IDepositDeviceService service)
        {
            _service = service;
        }

        public event EventHandler<string>? Escrowed;

        public async Task StartAsync(CancellationToken ct)
        {
            if (!_subscribed)
            {
                _service.Escrowed += OnEscrowed;
                _subscribed = true;
            }

            await _service.StartAsync(DeviceId, ct);
        }

        public async Task StopAsync(CancellationToken ct)
        {
            await _service.StopAsync(DeviceId, ct);
        }

        public Task StackAsync(CancellationToken ct) =>
            _service.StackAsync(DeviceId, ct);

        public Task ReturnAsync(CancellationToken ct) =>
            _service.ReturnAsync(DeviceId, ct);

        private void OnEscrowed(object? sender, DepositEscrowedEventArgs e)
        {
            if (!string.Equals(e.DeviceId, DeviceId, StringComparison.OrdinalIgnoreCase))
                return;

            Escrowed?.Invoke(this, e.Payload);
        }
    }
}
