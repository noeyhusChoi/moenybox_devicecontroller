using KIOSK.Device.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Status
{
    public interface IStatusStore
    {
        event Action<string, StatusSnapshot>? StatusUpdated;

        void Initialize(DeviceDescriptor desc); // Offline 초기 스냅샷
        void Update(string name, StatusSnapshot snapshot);

        IReadOnlyCollection<StatusSnapshot> GetAll();
        StatusSnapshot? TryGet(string name);
    }

    public class StatusStore : IStatusStore
    {
        // Latest snapshot cache keyed by device name.
        private readonly ConcurrentDictionary<string, StatusSnapshot> _snapshots = new();

        public event Action<string, StatusSnapshot>? StatusUpdated;

        public void Initialize(DeviceDescriptor desc)
        {
            // Emit an initial Offline snapshot so UI can show the device immediately.
            var snap = new StatusSnapshot
            {
                Name = desc.Name,
                Model = desc.Model,
                Health = DeviceHealth.Offline,
                Timestamp = DateTimeOffset.UtcNow
            };

            _snapshots[desc.Name] = snap;
            SafeInvokeStatusUpdated(desc.Name, snap);   // 처음부터 Offline 상태 알리기
        }

        public void Update(string name, StatusSnapshot snapshot)
        {
            if (_snapshots.TryGetValue(name, out var prev) && IsSameState(prev, snapshot))
                return;

            // Keep the newest snapshot by timestamp to avoid out-of-order writes.
            _snapshots.AddOrUpdate(name, snapshot,
                (_, prev) => snapshot.Timestamp >= prev.Timestamp ? snapshot : prev);

            SafeInvokeStatusUpdated(name, snapshot);
        }

        public IReadOnlyCollection<StatusSnapshot> GetAll()
            => _snapshots.Values
                         .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                         .ToArray();

        public StatusSnapshot? TryGet(string name)
            => _snapshots.TryGetValue(name, out var snap) ? snap : null;

        private void SafeInvokeStatusUpdated(string name, StatusSnapshot snapshot)
        {
            // Dispatch snapshot updates without allowing subscribers to break the store.
            var handlers = StatusUpdated;
            if (handlers is null)
                return;

            foreach (var del in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<string, StatusSnapshot>)del).Invoke(name, snapshot);
                }
                catch
                {
                    // store는 이벤트 소비자 예외에 영향을 받지 않아야 함
                }
            }
        }

        private static bool IsSameState(StatusSnapshot prev, StatusSnapshot current)
        {
            if (prev.Health != current.Health)
                return false;

            var prevKeys = GetAlarmKeys(prev.Alarms);
            var currKeys = GetAlarmKeys(current.Alarms);
            if (prevKeys.Count != currKeys.Count)
                return false;

            return prevKeys.SetEquals(currKeys);
        }

        private static HashSet<string> GetAlarmKeys(IReadOnlyCollection<StatusEvent>? alarms)
            => alarms is { Count: > 0 }
                ? new HashSet<string>(
                    alarms.Select(a => a.ErrorCode?.ToString() ?? a.Code ?? string.Empty)
                        .Where(k => !string.IsNullOrWhiteSpace(k)),
                    StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
