using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Status
{
    public sealed class DeviceStatusAdapter : IDeviceStatusPort
    {
        private readonly IDeviceManager _manager;
        private readonly IStatusStore _store;

        public DeviceStatusAdapter(IDeviceManager manager, IStatusStore store)
        {
            _manager = manager;
            _store = store;
        }

        public event Action<string, StatusSnapshot>? StatusUpdated
        {
            add => _store.StatusUpdated += value;
            remove => _store.StatusUpdated -= value;
        }

        public IReadOnlyCollection<StatusSnapshot> GetAllSnapshots()
            => _store.GetAll();

        public StatusSnapshot? TryGet(string deviceId)
            => _store.TryGet(deviceId);

        public IReadOnlyList<DeviceStatusInfo> GetDevices()
            => _manager.GetAllDevices()
                .Select(Map)
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public bool TryGetDevice(string deviceId, out DeviceStatusInfo info)
        {
            if (_manager.TryGetDevice(deviceId, out var device))
            {
                info = Map(device);
                return true;
            }

            info = default!;
            return false;
        }

        private static DeviceStatusInfo Map(DeviceRuntimeInfo device)
            => new(
                device.DeviceId,
                device.Name,
                device.Vendor,
                device.Model,
                device.TransportType,
                device.TransportPort,
                device.TransportParam,
                device.ProtocolName,
                device.PollingMs,
                device.DeviceType,
                device.Driver);
    }
}
