using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;

namespace DeviceController.Services
{
    public class DeviceRegistry : IDeviceRegistry
    {
        private readonly IReadOnlyList<IDevice> _devices;

        public DeviceRegistry(IEnumerable<IDevice> devices)
        {
            _devices = devices.ToList();
        }

        public IReadOnlyList<IDevice> Devices => _devices;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var device in _devices)
            {
                await device.StartAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var device in _devices)
            {
                await device.StopAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
