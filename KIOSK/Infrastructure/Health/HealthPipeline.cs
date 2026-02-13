using System.Diagnostics;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Status;

namespace KIOSK.Infrastructure.Health;

public interface IHealthPipeline
{
    void Process(HealthSignal signal);
}

/// <summary>
/// 공통 상태 허브.
/// 현재는 Device 소스만 처리하며, 추후 Database/Network/Disk 소스를 동일 경로로 확장한다.
/// </summary>
public sealed class HealthPipeline : IHealthPipeline
{
    private readonly IStatusStore _deviceStore;
    private readonly IStatusNotifyService _deviceNotifier;
    private readonly IStatusLogService _deviceRepository;
    private readonly IErrorPolicy _policy;
    private readonly IErrorMessageProvider _messages;

    public HealthPipeline(
        IStatusStore deviceStore,
        IStatusNotifyService deviceNotifier,
        IStatusLogService deviceRepository,
        IErrorPolicy policy,
        IErrorMessageProvider messages)
    {
        _deviceStore = deviceStore;
        _deviceNotifier = deviceNotifier;
        _deviceRepository = deviceRepository;
        _policy = policy;
        _messages = messages;
    }

    public void Process(HealthSignal signal)
    {
        switch (signal.SourceKind)
        {
            case HealthSourceKind.Device:
                ProcessCore(signal.SourceId, signal.Snapshot, persistLog: true);
                break;
            case HealthSourceKind.Network:
            case HealthSourceKind.Disk:
                ProcessCore(signal.SourceId, signal.Snapshot, persistLog: false);
                break;
            default:
                ProcessCore(signal.SourceId, signal.Snapshot, persistLog: false);
                break;
        }
    }

    private void ProcessCore(string sourceId, StatusSnapshot snapshot, bool persistLog)
    {
        var prev = _deviceStore.TryGet(sourceId);
        if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
        {
            _deviceStore.TryUpdate(sourceId, snapshot, out _);
            return;
        }

        var prevKeys = GetAlertKeys(prev?.Alerts);
        var (health, alerts) = NormalizeAlerts(sourceId, snapshot, prevKeys);

        var normalizedSnapshot = snapshot with { Health = health, Alerts = alerts };
        if (!_deviceStore.TryUpdate(sourceId, normalizedSnapshot, out var storedSnapshot))
            return;

        var publishable = FilterPublishableAlerts(storedSnapshot, prevKeys);
        if (publishable.Alerts is { Count: > 0 })
            _ = PublishSafeAsync(sourceId, publishable);

        if (!persistLog)
            return;

        var storable = FilterStorableAlerts(storedSnapshot, prevKeys);
        if (storable.Alerts is { Count: > 0 })
            _ = SaveSafeAsync(sourceId, storable);
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
            var isDuplicate = prevKeys.Contains(alertKey);

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
            await _deviceNotifier.PublishAsync(name, snapshot).ConfigureAwait(false);
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
            await _deviceRepository.SaveAsync(name, snapshot).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[StatusRepository] {name} save failed: {ex.Message}");
        }
    }
}
