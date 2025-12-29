using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 공통 장치 동작 패턴(스냅샷 생성, I/O 직렬화, 트랜스포트 보조)을 제공하는 기본 클래스.
/// 개별 장치는 필요한 부분만 오버라이드하여 구현하면 됩니다.
/// </summary>
public abstract class DeviceBase : IDevice, IAsyncDisposable
{
    private readonly SemaphoreSlim? _ioGate;

    protected DeviceBase(DeviceDescriptor descriptor, ITransport? transport, bool enableIoSerialization = true)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Transport = transport;

        if (enableIoSerialization)
            _ioGate = new SemaphoreSlim(1, 1);
    }

    public string Name => Descriptor.Name;
    public string Model => Descriptor.Model;

    protected DeviceDescriptor Descriptor { get; }
    protected ITransport? Transport { get; }

    protected StatusSnapshot CreateSnapshot(IEnumerable<StatusEvent>? alarms = null)
        => new()
        {
            Name = Name,
            Model = Model,
            Health = DeviceHealth.Online,
            Timestamp = DateTimeOffset.UtcNow,
            Alarms = alarms?.ToList() ?? new List<StatusEvent>()
        };

    protected StatusEvent CreateAlarm(string code, string message, Severity severity = Severity.Error)
        => new(code, message, severity, DateTimeOffset.UtcNow);

    protected StatusEvent CreateAlarm(ErrorCode code, string message, Severity severity = Severity.Error)
        => new(code.ToString(), string.Empty, severity, DateTimeOffset.UtcNow, ErrorCode: code);

    protected async Task<IDisposable> AcquireIoAsync(CancellationToken ct)
    {
        if (_ioGate is null)
            return NullDisposable.Instance;

        await _ioGate.WaitAsync(ct).ConfigureAwait(false);
        return new Releaser(_ioGate);
    }

    protected Task EnsureTransportOpenAsync(CancellationToken ct)
    {
        if (Transport is null)
            return Task.CompletedTask;

        return Transport.IsOpen
            ? Task.CompletedTask
            : Transport.OpenAsync(ct);
    }

    protected ITransport RequireTransport()
        => Transport ?? throw new InvalidOperationException($"{GetType().Name} has no transport assigned.");

    protected TransportChannel CreateChannel(IFramer? framer = null)
        => new TransportChannel(RequireTransport(), framer);

    public abstract Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default);
    public abstract Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default);
    public abstract Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default);

    public virtual ValueTask DisposeAsync()
    {
        _ioGate?.Dispose();
        return Transport?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _semaphore.Release();
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        private NullDisposable() { }
        public void Dispose() { }
    }
}
