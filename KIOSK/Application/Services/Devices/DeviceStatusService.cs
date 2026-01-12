using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Infrastructure.Management.Status;

namespace KIOSK.Application.Services.Devices
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

        public StatusSnapshot? TryGet(string name)
            => _store.TryGet(name);

        public IReadOnlyList<DeviceStatusInfo> GetDevices()
            => _host.GetAllSupervisors()
                .Select(Map)
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public bool TryGetDevice(string name, out DeviceStatusInfo info)
        {
            if (_host.TryGetSupervisor(name, out var sup))
            {
                info = Map(sup);
                return true;
            }

            info = default!;
            return false;
        }

        private static DeviceStatusInfo Map(DeviceSupervisor sup)
            => new(
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
