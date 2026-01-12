using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using KIOSK.Device.Abstractions;

namespace KIOSK.Infrastructure.Management.Status;

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
    private readonly IStatusNotifyService _notifier;
    private readonly IStatusLogService _repository;
    private readonly IErrorPolicy _policy;
    private readonly IErrorMessageProvider _messages;

    public StatusPipeline(
        IStatusStore store,
        IStatusNotifyService notifier,
        IStatusLogService repository,
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
        var prev = _store.TryGet(name);
        if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
        {
            _store.TryUpdate(name, snapshot, out _);
            return;
        }

        var prevKeys = GetAlertKeys(prev?.Alerts);
        var (health, alerts) = NormalizeAlerts(name, snapshot, prevKeys);

        var normalizedSnapshot = snapshot with { Health = health, Alerts = alerts };
        if (!_store.TryUpdate(name, normalizedSnapshot, out var storedSnapshot))
            return;

        var publishable = FilterPublishableAlerts(storedSnapshot, prevKeys);
        if (publishable.Alerts is { Count: > 0 })
            _ = PublishSafeAsync(name, publishable);

        var storable = FilterStorableAlerts(storedSnapshot, prevKeys);
        if (storable.Alerts is { Count: > 0 })
            _ = SaveSafeAsync(name, storable);
    }

    private static string GetAlertKey(StatusEvent alert)
        => $"{alert.Source}:{alert.ErrorCode?.ToString() ?? alert.Code ?? string.Empty}";

    private static HashSet<string> GetAlertKeys(IReadOnlyCollection<StatusEvent>? alerts)
        => alerts is { Count: > 0 }
            ? new HashSet<string>(
                alerts.Select(GetAlertKey).Where(k => !string.IsNullOrWhiteSpace(k)),
                StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static bool IsKnownAlert(HashSet<string> prevKeys, string alertKey)
        => string.IsNullOrWhiteSpace(alertKey) || prevKeys.Contains(alertKey);

    private (DeviceHealth health, List<StatusEvent> alerts) NormalizeAlerts(
        string name,
        StatusSnapshot snapshot,
        HashSet<string> prevKeys)
    {
        var alerts = new List<StatusEvent>(snapshot.Alerts!.Count);
        var health = snapshot.Health;

        foreach (var alert in snapshot.Alerts!)
        {
            var alertKey = GetAlertKey(alert);
            var isDuplicate = prevKeys?.Contains(alertKey) == true;

            if (alert.ErrorCode is null && ErrorCode.TryParse(alert.Code, out var parsed))
            {
                var policy = _policy.Apply(parsed);
                if (policy.SetOffline)
                    health = DeviceHealth.Offline;

                var message = _messages.GetMessage(parsed) ?? string.Empty;
                var isSupportedScope = IsSupportedScope(alert.Source);
                var normalized = alert with
                {
                    ErrorCode = parsed,
                    Notify = policy.Notify && isSupportedScope && !IsKnownAlert(prevKeys, alertKey),
                    Severity = policy.SeverityOverride ?? alert.Severity,
                    Message = message
                };
                if (!isDuplicate)
                    Trace.WriteLine($"[StatusEvent][{name}] {normalized.Severity} {normalized.Code}: {normalized.Message}");
                alerts.Add(normalized);
                continue;
            }

            if (alert.ErrorCode is { } code)
            {
                var policy = _policy.Apply(code);
                if (policy.SetOffline)
                    health = DeviceHealth.Offline;

                var message = _messages.GetMessage(code) ?? string.Empty;
                var isSupportedScope = IsSupportedScope(alert.Source);
                var normalized = alert with
                {
                    Notify = policy.Notify && isSupportedScope && !IsKnownAlert(prevKeys, alertKey),
                    Severity = policy.SeverityOverride ?? alert.Severity,
                    Message = message
                };
                if (!isDuplicate)
                    Trace.WriteLine($"[StatusEvent][{name}] {normalized.Severity} {normalized.Code}: {normalized.Message}");
                alerts.Add(normalized);
                continue;
            }

            if (!isDuplicate)
                Trace.WriteLine($"[StatusEvent][{name}] {alert.Severity} {alert.Code}: {alert.Message}");
            alerts.Add(alert with { Notify = false });
        }

        return (health, alerts);
    }

    private static bool IsSupportedScope(AlertSource source)
        => source is AlertSource.Connection or AlertSource.Status;

    private static StatusSnapshot FilterPublishableAlerts(StatusSnapshot snapshot, HashSet<string> prevKeys)
    {
        if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
            return snapshot with { Alerts = new List<StatusEvent>() };

        var alerts = snapshot.Alerts
            .Where(a => a.Notify)
            .Where(a => IsSupportedScope(a.Source))
            .Where(a => !IsKnownAlert(prevKeys, GetAlertKey(a)))
            .ToList();

        return snapshot with { Alerts = alerts };
    }

    private static StatusSnapshot FilterStorableAlerts(StatusSnapshot snapshot, HashSet<string> prevKeys)
    {
        if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
            return snapshot with { Alerts = new List<StatusEvent>() };

        var alerts = snapshot.Alerts
            .Where(a => IsSupportedScope(a.Source))
            .Where(a => !IsKnownAlert(prevKeys, GetAlertKey(a)))
            .ToList();

        return snapshot with { Alerts = alerts };
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
