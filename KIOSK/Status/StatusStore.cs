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
        bool TryUpdate(string name, StatusSnapshot snapshot, out StatusSnapshot effectiveSnapshot);

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

        public bool TryUpdate(string name, StatusSnapshot snapshot, out StatusSnapshot effectiveSnapshot)
        {
            var prev = _snapshots.TryGetValue(name, out var existing) ? existing : null;
            var merged = MergeByScope(prev, snapshot);
            if (prev is not null && IsSameState(prev, merged))
            {
                effectiveSnapshot = prev;
                return false;
            }

            // Keep the newest snapshot by timestamp to avoid out-of-order writes.
            _snapshots.AddOrUpdate(name, merged,
                (_, prevSnap) => merged.Timestamp >= prevSnap.Timestamp ? merged : prevSnap);

            effectiveSnapshot = _snapshots[name];
            SafeInvokeStatusUpdated(name, effectiveSnapshot);
            return true;
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

            if (prev.AlertScope != current.AlertScope)
                return false;

            var prevKeys = GetAlertKeys(prev.Alerts);
            var currKeys = GetAlertKeys(current.Alerts);
            if (prevKeys.Count != currKeys.Count)
                return false;

            return prevKeys.SetEquals(currKeys);
        }

        private static HashSet<string> GetAlertKeys(IReadOnlyCollection<StatusEvent>? alerts)
            => alerts is { Count: > 0 }
                ? new HashSet<string>(
                    alerts.Select(a => $"{a.Source}:{a.ErrorCode?.ToString() ?? a.Code ?? string.Empty}")
                        .Where(k => !string.IsNullOrWhiteSpace(k)),
                    StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static StatusSnapshot MergeByScope(StatusSnapshot? prev, StatusSnapshot current)
        {
            var scope = current.AlertScope;
            var currentAlerts = NormalizeByScope(current.Alerts, scope);

            if (prev is null || prev.Alerts is null || prev.Alerts.Count == 0)
                return current with { Alerts = currentAlerts };

            var prevAlerts = prev.Alerts;

            // 연결 끊김(Offline)인데 새 알림이 비어있으면 기존 연결 알림은 유지한다.
            if (scope == AlertSource.Connection
                && current.Health == DeviceHealth.Offline
                && currentAlerts.Count == 0)
            {
                return current with { Alerts = prevAlerts.ToList() };
            }

            var merged = prevAlerts
                .Where(a => a.Source != scope)
                .Concat(currentAlerts)
                .ToList();

            return current with { Alerts = merged };
        }

        private static List<StatusEvent> NormalizeByScope(IReadOnlyCollection<StatusEvent>? alerts, AlertSource scope)
        {
            if (alerts is null || alerts.Count == 0)
                return new List<StatusEvent>();

            var list = new List<StatusEvent>(alerts.Count);
            foreach (var alert in alerts)
            {
                list.Add(alert.Source == scope ? alert : alert with { Source = scope });
            }

            return list;
        }
    }
}
