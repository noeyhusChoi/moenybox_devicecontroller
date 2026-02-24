using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Drivers;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class DepositAdapter : IDepositPort
    {
        private readonly IDeviceManager _deviceManager;
        private readonly Dictionary<string, IDepositDriver> _drivers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EventHandler<string>> _handlers = new(StringComparer.OrdinalIgnoreCase);

        public DepositAdapter(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public event EventHandler<DepositEscrowedEventArgs>? Escrowed;

        public async Task StartAsync(string deviceId, CancellationToken ct)
        {
            if (_deviceManager.TryGetInnerDevice<IDepositDriver>(deviceId, out var driver))
            {
                _drivers[deviceId] = driver;
                if (!_handlers.TryGetValue(deviceId, out var handler))
                {
                    handler = (_, payload) => Escrowed?.Invoke(this, new DepositEscrowedEventArgs(deviceId, payload));
                    _handlers[deviceId] = handler;
                }
                driver.Escrowed += handler;

                await driver.StartAcceptanceAsync(ct).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(string deviceId, CancellationToken ct)
        {
            if (_drivers.TryGetValue(deviceId, out var driver))
            {
                if (_handlers.TryGetValue(deviceId, out var handler))
                    driver.Escrowed -= handler;
                _drivers.Remove(deviceId);
            }

            if (_deviceManager.TryGetInnerDevice<IDepositDriver>(deviceId, out var stopDriver))
            {
                await stopDriver.StopAcceptanceAsync(ct).ConfigureAwait(false);
            }
        }

        public async Task StackAsync(string deviceId, CancellationToken ct)
        {
            if (_deviceManager.TryGetInnerDevice<IDepositDriver>(deviceId, out var driver))
            {
                await driver.StackAsync(ct).ConfigureAwait(false);
            }
        }

        public async Task ReturnAsync(string deviceId, CancellationToken ct)
        {
            if (_deviceManager.TryGetInnerDevice<IDepositDriver>(deviceId, out var driver))
            {
                await driver.ReturnAsync(ct).ConfigureAwait(false);
            }
        }
    }
}
