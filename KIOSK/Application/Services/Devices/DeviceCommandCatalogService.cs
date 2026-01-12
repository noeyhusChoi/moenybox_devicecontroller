using KIOSK.Infrastructure.Management.Devices;

namespace KIOSK.Application.Services.Devices
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
