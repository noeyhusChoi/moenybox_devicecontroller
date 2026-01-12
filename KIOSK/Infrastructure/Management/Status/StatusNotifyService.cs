using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Infrastructure.Management.Status;

public interface IStatusNotifyService
{
    Task PublishAsync(string name, StatusSnapshot snapshot);
}

public sealed class StatusNotifyService : IStatusNotifyService, IDisposable
{
    private readonly TimeSpan _window = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, StatusEventAggregate>> _buffer =
        new(StringComparer.OrdinalIgnoreCase);
    private Timer? _timer;

    public Task PublishAsync(string name, StatusSnapshot snapshot)
    {
        var alerts = snapshot.Alerts?.Where(a => a.Notify).ToList();
        if (alerts is null || alerts.Count == 0)
            return Task.CompletedTask;

        lock (_gate)
        {
            if (!_buffer.TryGetValue(name, out var perDevice))
            {
                perDevice = new Dictionary<string, StatusEventAggregate>(StringComparer.OrdinalIgnoreCase);
                _buffer[name] = perDevice;
            }

            foreach (var alert in alerts)
            {
                var key = alert.ErrorCode?.ToString() ?? alert.Code ?? "UNKNOWN";
                if (perDevice.TryGetValue(key, out var agg))
                {
                    agg.Count++;
                    agg.LastSeverity = alert.Severity;
                    agg.LastMessage = alert.Message;
                    perDevice[key] = agg;
                    continue;
                }

                perDevice[key] = new StatusEventAggregate(1, alert.Severity, alert.Message);
            }

            EnsureTimer();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            _buffer.Clear();
        }
    }

    private void EnsureTimer()
    {
        if (_timer is not null)
            return;

        _timer = new Timer(_ => Flush(), null, _window, Timeout.InfiniteTimeSpan);
    }

    private void Flush()
    {
        Dictionary<string, Dictionary<string, StatusEventAggregate>> snapshot;
        lock (_gate)
        {
            if (_buffer.Count == 0)
            {
                _timer?.Dispose();
                _timer = null;
                return;
            }

            snapshot = new Dictionary<string, Dictionary<string, StatusEventAggregate>>(_buffer, StringComparer.OrdinalIgnoreCase);
            _buffer.Clear();
            _timer?.Dispose();
            _timer = null;
        }

        foreach (var (device, events) in snapshot)
        {
            foreach (var (code, agg) in events)
            {
                Trace.WriteLine($"[StatusNotifier] {device} {code} x{agg.Count} {agg.LastSeverity} {agg.LastMessage}");
            }
        }
    }

    private sealed class StatusEventAggregate
    {
        public int Count { get; set; }
        public Severity LastSeverity { get; set; }
        public string LastMessage { get; set; }

        public StatusEventAggregate(int count, Severity lastSeverity, string lastMessage)
        {
            Count = count;
            LastSeverity = lastSeverity;
            LastMessage = lastMessage;
        }
    }
}

public sealed class NullStatusNotifyService : IStatusNotifyService
{
    public Task PublishAsync(string name, StatusSnapshot snapshot)
    {
        return Task.CompletedTask;
    }
}
