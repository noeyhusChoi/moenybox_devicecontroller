using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Database;
using System.Diagnostics;

namespace KIOSK.Services
{
    public class DeviceErrorEventService
    {
        private readonly IDatabaseService _db;
        //private readonly IErrorRepository _repo;
        //private readonly NotificationDispatcher _dispatcher;

        private readonly Dictionary<string, HashSet<string>> _snapshot = new();
        private readonly HashSet<string> _initialized = new();

        public DeviceErrorEventService(IDatabaseService db)
        {
            _db = db;
        }

        public async Task OnStatusUpdated(string deviceId, StatusSnapshot snap)
        {
            // snap.Alarms: DeviceStatusSnapshot 이 가지고 있는 현재 발생중인 알람(ErrorCode)
            // e.Code: 알람의 ErrorCode 문자열
            var current = (snap.Alerts ?? new List<StatusEvent>()).Select(e => e.Code).ToHashSet();
            bool isOffline = snap.Health == DeviceHealth.Offline;

            // 이전에 저장된 snapshot 가져오기 (없으면 빈 HashSet)
            var prev = _snapshot.GetValueOrDefault(deviceId) ?? new();

            // 프로그램 시작 후 첫 스냅샷은 기준점만 기록하고 알림은 보내지 않는다.
            if (!_initialized.Contains(deviceId))
            {
                _snapshot[deviceId] = current;
                _initialized.Add(deviceId);
                return;
            }

            // 신규 에러: 이전에는 없었는데 지금 있는 에러들
            var newErr = current.Except(prev);

            // 해제된 에러: 이전에는 있었는데 지금 사라진 에러들
            var clrErr = prev.Except(current);

            // Offline 상태에서는 클리어 이벤트를 보내지 않는다(알람 유지).
            if (isOffline)
                clrErr = Array.Empty<string>();

            // DB/Repo 연동 대신 Trace로 대체
            foreach (var code in newErr)
            {
                Trace.WriteLine($"[DeviceError][INSERT] {deviceId} {code}");
                Trace.WriteLine($"[DeviceError][NOTIFY] {deviceId} {code}");
                Trace.WriteLine($"[DeviceError][MARK_SENT] {deviceId} {code}");
            }

            foreach (var code in clrErr)
            {
                Trace.WriteLine($"[DeviceError][CLEAR] {deviceId} {code}");
                Trace.WriteLine($"[DeviceError][NOTIFY_CLEAR] {deviceId} {code}");
                Trace.WriteLine($"[DeviceError][MARK_CLEAR] {deviceId} {code}");
            }

            // Snapshot 업데이트
            _snapshot[deviceId] = current;
        }

        private async Task Notify(string deviceId, string code)
        {
            Trace.WriteLine($"ERROR NOTIFY {deviceId} {code}");
            //var ctx = await _repo.BuildContext(deviceId, code);
            //await _dispatcher.DispatchAsync(ctx);
            //await _repo.InsertErrorAsync(deviceId, code);
        }
    }
}
