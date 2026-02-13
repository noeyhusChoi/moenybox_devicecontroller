using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime;

namespace KIOSK.Infrastructure.Devices.Status
{
    public sealed class DeviceStatusService : IDeviceStatusService
    {
        private readonly IDeviceHost _host;
        private readonly IStatusStore _store;

        public DeviceStatusService(IDeviceHost host, IStatusStore store)
        {
            _host = host;
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
            => _host.GetAllSupervisors()
                .Select(Map)
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public bool TryGetDevice(string deviceId, out DeviceStatusInfo info)
        {
            if (_host.TryGetSupervisor(deviceId, out var sup))
            {
                info = Map(sup);
                return true;
            }

            info = default!;
            return false;
        }

        private static DeviceStatusInfo Map(DeviceSupervisor sup)
            => new(
                sup.DeviceId,
                sup.Name,
                sup.Vendor,
                sup.Model,
                sup.TransportType,
                sup.TransportPort,
                sup.TransportParam,
                sup.ProtocolName,
                sup.PollingMs,
                sup.DeviceType,
                sup.Driver);
    }
}
