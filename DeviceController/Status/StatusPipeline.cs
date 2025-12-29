using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Status;

public interface IStatusPipeline
{
    void Process(string name, StatusSnapshot snapshot);
}

/// <summary>
/// 상태 스냅샷 처리 파이프라인(필터/중복제거/정책 확장 포인트).
/// </summary>
public sealed class StatusPipeline : IStatusPipeline
{
    private readonly IStatusStore _store;
    private readonly IStatusNotifier _notifier;
    private readonly IStatusRepository _repository;
    private readonly IErrorPolicy _policy;
    private readonly IErrorMessageProvider _messages;

    public StatusPipeline(
        IStatusStore store,
        IStatusNotifier notifier,
        IStatusRepository repository,
        IErrorPolicy policy,
        IErrorMessageProvider messages)
    {
        _store = store;
        _notifier = notifier;
        _repository = repository;
        _policy = policy;
        _messages = messages;
    }

    public void Process(string name, StatusSnapshot snapshot)
    {
        // 알람 정규화/정책 적용 후 Store/Notifier/Repository로 전달한다.
        var prev = _store.TryGet(name);
        if (IsSameState(prev, snapshot))
            return;

        // 알람이 없으면 정규화 없이 스냅샷을 그대로 저장한다.
        if (snapshot.Alarms is null || snapshot.Alarms.Count == 0)
        {
            _store.Update(name, snapshot);
            return;
        }

        var prevKeys = GetAlarmKeys(prev?.Alarms);

        var alarms = new List<StatusEvent>(snapshot.Alarms.Count);
        var health = snapshot.Health;

        foreach (var alarm in snapshot.Alarms)
        {
            var alarmKey = GetAlarmKey(alarm);
            var isDuplicate = prevKeys?.Contains(alarmKey) == true;
            // 정책/알림 판단을 위해 ErrorCode를 우선 사용한다.
            // 문자열 코드 경로: ErrorCode로 파싱 후 정책 적용.
            if (alarm.ErrorCode is null && ErrorCode.TryParse(alarm.Code, out var parsed))
            {
                var policy = _policy.Apply(parsed);
                if (policy.SetOffline)
                    health = DeviceHealth.Offline;

                var message = _messages.GetMessage(parsed) ?? string.Empty;
                // 정책이 Severity/Notify를 변경할 수 있으며, 메시지는 여기서 정규화한다.
                var normalized = alarm with
                {
                    ErrorCode = parsed,
                    Notify = policy.Notify && !IsKnownAlarm(prevKeys, alarmKey),
                    Severity = policy.SeverityOverride ?? alarm.Severity,
                    Message = message
                };
                if (!isDuplicate)
                    Trace.WriteLine($"[StatusEvent][{name}] {normalized.Severity} {normalized.Code}: {normalized.Message}");
                alarms.Add(normalized);
                continue;
            }

            // ErrorCode가 이미 있으면 바로 정책 적용.
            if (alarm.ErrorCode is { } code)
            {
                var policy = _policy.Apply(code);
                if (policy.SetOffline)
                    health = DeviceHealth.Offline;

                var message = _messages.GetMessage(code) ?? string.Empty;
                // 코드 자체는 유지하고 Severity/Notify/Message만 일관되게 정규화한다.
                var normalized = alarm with
                {
                    Notify = policy.Notify && !IsKnownAlarm(prevKeys, alarmKey),
                    Severity = policy.SeverityOverride ?? alarm.Severity,
                    Message = message
                };
                if (!isDuplicate)
                    Trace.WriteLine($"[StatusEvent][{name}] {normalized.Severity} {normalized.Code}: {normalized.Message}");
                alarms.Add(normalized);
                continue;
            }

            if (!isDuplicate)
                Trace.WriteLine($"[StatusEvent][{name}] {alarm.Severity} {alarm.Code}: {alarm.Message}");
            alarms.Add(alarm);
        }

        var normalizedSnapshot = snapshot with { Health = health, Alarms = alarms };
        // Store는 현재 UI 상태의 단일 기준(source of truth)이다.
        _store.Update(name, normalizedSnapshot);

        // fire-and-forget: 하위 실패가 장치 루프를 막지 않게 한다.
        _ = PublishSafeAsync(name, normalizedSnapshot);
        _ = SaveSafeAsync(name, normalizedSnapshot);
    }

    private static string GetAlarmKey(StatusEvent alarm)
        => alarm.ErrorCode?.ToString() ?? alarm.Code ?? string.Empty;

    private static HashSet<string> GetAlarmKeys(IReadOnlyCollection<StatusEvent>? alarms)
        => alarms is { Count: > 0 }
            ? new HashSet<string>(
                alarms.Select(GetAlarmKey).Where(k => !string.IsNullOrWhiteSpace(k)),
                StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static bool IsKnownAlarm(HashSet<string> prevKeys, string alarmKey)
        => string.IsNullOrWhiteSpace(alarmKey) || prevKeys.Contains(alarmKey);

    private static bool IsSameState(StatusSnapshot? prev, StatusSnapshot current)
    {
        if (prev is null)
            return false;

        if (prev.Health != current.Health)
            return false;

        var prevKeys = GetAlarmKeys(prev.Alarms);
        var currKeys = GetAlarmKeys(current.Alarms);
        if (prevKeys.Count != currKeys.Count)
            return false;

        return prevKeys.SetEquals(currKeys);
    }

    private async Task PublishSafeAsync(string name, StatusSnapshot snapshot)
    {
        try
        {
            await _notifier.PublishAsync(name, snapshot).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[StatusNotifier] {name} publish failed: {ex.Message}");
        }
    }

    private async Task SaveSafeAsync(string name, StatusSnapshot snapshot)
    {
        try
        {
            await _repository.SaveAsync(name, snapshot).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[StatusRepository] {name} save failed: {ex.Message}");
        }
    }
}
