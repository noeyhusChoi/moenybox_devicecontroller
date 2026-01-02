using KIOSK.Device.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Devices.Management
{
    public interface IDeviceStatusStore
    {
        event Action<string, DeviceStatusSnapshot>? StatusUpdated;

        void Initialize(DeviceDescriptor desc);                 // Offline 초기 스냅샷
        void Update(string name, DeviceStatusSnapshot snapshot);

        IReadOnlyCollection<DeviceStatusSnapshot> GetAll();
        DeviceStatusSnapshot? TryGet(string name);
    }

    public class DeviceStatusStore : IDeviceStatusStore
    {
        private readonly ConcurrentDictionary<string, DeviceStatusSnapshot> _snapshots = new();

        public event Action<string, DeviceStatusSnapshot>? StatusUpdated;

        public void Initialize(DeviceDescriptor desc)
        {
            var snap = new DeviceStatusSnapshot
            {
                Name = desc.Name,
                Model = desc.Model,
                Health = DeviceHealth.Offline,
                Timestamp = DateTimeOffset.UtcNow
            };

            _snapshots[desc.Name] = snap;
            StatusUpdated?.Invoke(desc.Name, snap);   // 처음부터 Offline 상태 알리기
        }

        public void Update(string name, DeviceStatusSnapshot snapshot)
        {
            _snapshots.AddOrUpdate(name, snapshot,
                (_, prev) => snapshot.Timestamp >= prev.Timestamp ? snapshot : prev);

            StatusUpdated?.Invoke(name, snapshot);
        }

        public IReadOnlyCollection<DeviceStatusSnapshot> GetAll()
            => _snapshots.Values
                         .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                         .ToArray();

        public DeviceStatusSnapshot? TryGet(string name)
            => _snapshots.TryGetValue(name, out var snap) ? snap : null;
    }
}
