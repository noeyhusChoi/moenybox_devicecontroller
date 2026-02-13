using KIOSK.Application.Services.Devices;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Control
{
    public sealed class DeviceCommandCatalogService : IDeviceCommandCatalogService
    {
        private readonly IDeviceCommandCatalog _catalog;

        public DeviceCommandCatalogService(IDeviceCommandCatalog catalog)
        {
            _catalog = catalog;
        }

        public IEnumerable<DeviceCommandInfo> GetFor(string deviceName)
            => _catalog.GetFor(deviceName)
                .Select(cmd => new DeviceCommandInfo(cmd.Name, cmd.Description));
    }
}
