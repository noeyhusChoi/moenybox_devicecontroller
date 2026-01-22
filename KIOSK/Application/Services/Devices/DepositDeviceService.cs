using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Device.Drivers;
using KIOSK.Infrastructure.Management.Devices;

namespace KIOSK.Application.Services.Devices
{
    public sealed class DepositDeviceService : IDepositDeviceService
    {
        private readonly IDeviceManager _deviceManager;
        private readonly Dictionary<string, DepositDriver> _drivers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EventHandler<string>> _handlers = new(StringComparer.OrdinalIgnoreCase);

        public DepositDeviceService(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public event EventHandler<DepositEscrowedEventArgs>? Escrowed;

        public async Task StartAsync(string deviceId, CancellationToken ct)
        {
            var deposit = _deviceManager.GetDevice<IDevice>(deviceId);
            if (deposit is DepositDriver driver)
            {
                _drivers[deviceId] = driver;
                if (!_handlers.TryGetValue(deviceId, out var handler))
                {
                    handler = (_, payload) => Escrowed?.Invoke(this, new DepositEscrowedEventArgs(deviceId, payload));
                    _handlers[deviceId] = handler;
                }
                driver.OnEscrowed += handler;
            }

            await _deviceManager.SendAsync(deviceId, new DeviceCommand("Start"), ct);
        }

        public async Task StopAsync(string deviceId, CancellationToken ct)
        {
            if (_drivers.TryGetValue(deviceId, out var driver))
            {
                if (_handlers.TryGetValue(deviceId, out var handler))
                    driver.OnEscrowed -= handler;
                _drivers.Remove(deviceId);
            }

            await _deviceManager.SendAsync(deviceId, new DeviceCommand("Stop"), ct);
        }

        public Task StackAsync(string deviceId, CancellationToken ct) =>
            _deviceManager.SendAsync(deviceId, new DeviceCommand("Stack"), ct);

        public Task ReturnAsync(string deviceId, CancellationToken ct) =>
            _deviceManager.SendAsync(deviceId, new DeviceCommand("Return"), ct);
    }
}
