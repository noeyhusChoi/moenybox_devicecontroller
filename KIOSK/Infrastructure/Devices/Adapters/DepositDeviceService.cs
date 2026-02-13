using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime.Factories;
using KIOSK.Device.Drivers;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Adapters
{
    public sealed class DepositDeviceService : IDepositDeviceService
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IDeviceHost _deviceHost;
        private readonly Dictionary<string, DepositDriver> _drivers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EventHandler<string>> _handlers = new(StringComparer.OrdinalIgnoreCase);

        public DepositDeviceService(IDeviceManager deviceManager, IDeviceHost deviceHost)
        {
            _deviceManager = deviceManager;
            _deviceHost = deviceHost;
        }

        public event EventHandler<DepositEscrowedEventArgs>? Escrowed;

        public async Task StartAsync(string deviceId, CancellationToken ct)
        {
            if (_deviceHost.TryGetSupervisor(deviceId, out var sup) &&
                sup.GetInnerDevice<DepositDriver>() is { } driver)
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
