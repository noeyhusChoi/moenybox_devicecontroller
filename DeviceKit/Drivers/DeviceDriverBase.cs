using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeviceKit.Drivers;

/// <summary>
/// 공통 장치 동작 패턴(스냅샷 생성, I/O 직렬화, 명령 디스패치)을 제공하는 기본 클래스.
/// 개별 장치는 필요한 부분만 오버라이드하여 구현하면 됩니다.
/// </summary>
internal abstract class DeviceDriverBase : IDeviceDriver, IDeviceEventSource, IAsyncDisposable
{
    private readonly SemaphoreSlim? _ioGate;

    protected DeviceDriverBase(DeviceDescriptor descriptor, ILogger logger, bool enableIoSerialization = true)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (enableIoSerialization)
            _ioGate = new SemaphoreSlim(1, 1);
    }

    public string Name => Descriptor.Name;
    public string Model => Descriptor.Model;

    public event EventHandler<DeviceDriverEvent>? EventOccurred;

    protected DeviceDescriptor Descriptor { get; }
    protected ILogger Logger { get; }
    protected abstract string ErrorTarget { get; }
    protected abstract IReadOnlyDictionary<string, DeviceCommandSpec> Commands { get; }
    protected abstract bool IsCommandReady { get; }

    protected StatusSnapshot CreateSnapshot(IEnumerable<StatusEvent>? alerts = null)
        => new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            Alerts = alerts?.ToList() ?? new List<StatusEvent>()
        };

    protected StatusEvent CreateAlert(ErrorCode code, string message, Severity severity = Severity.Error)
        => new(code.ToString(), message, severity, DateTimeOffset.UtcNow, ErrorCode: code);

    protected async Task<IDisposable> AcquireIoAsync(CancellationToken ct)
    {
        if (_ioGate is null)
            return NullDisposable.Instance;

        await _ioGate.WaitAsync(ct).ConfigureAwait(false);
        return new Releaser(_ioGate);
    }

    protected void PublishDriverEvent(string eventName, object? payload = null, int version = 1)
    {
        try
        {
            EventOccurred?.Invoke(this, new DeviceDriverEvent(eventName, payload, version));
        }
        catch
        {
        }
    }

    public abstract Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default);
    public abstract Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default);
    public virtual async Task<DeviceCommandResponse> ExecuteAsync(DeviceCommandRequest command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            if (!IsCommandReady)
                return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED"));

            if (string.IsNullOrWhiteSpace(command.Name))
                return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "UNKNOWN_COMMAND"));

            return await ExecuteCommandByNameAsync(command, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning(ex, "{Driver} command timeout. device={Device} command={Command}", GetType().Name, Name, command.Name);
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "TIMEOUT"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Driver} command failed. device={Device} command={Command}", GetType().Name, Name, command.Name);
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "ERROR"));
        }
    }

    public virtual ValueTask DisposeAsync()
    {
        _ioGate?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected Task<DeviceCommandResponse> ExecuteCommandByNameAsync(DeviceCommandRequest command, CancellationToken ct)
    {
        if (!Commands.TryGetValue(command.Name.Trim(), out var spec))
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "UNKNOWN_COMMAND")));

        if (!spec.IsPayloadValid(command.Payload))
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "INVALID_PAYLOAD")));

        return spec.ExecuteAsync(this, command, ct);
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
