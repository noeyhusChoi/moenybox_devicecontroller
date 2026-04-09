using DeviceKit.Engine;

namespace DeviceController.Services;

public sealed class DeferredDeviceManagerPort : IDeviceManagerPort, IAsyncDisposable
{
    private readonly IReadOnlyList<DeviceDescriptor> _descriptors;
    private readonly DeviceRuntimeOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DeviceRuntimePort? _inner;

    public DeferredDeviceManagerPort(IReadOnlyList<DeviceDescriptor> descriptors, DeviceRuntimeOptions options)
    {
        _descriptors = descriptors;
        _options = options;
    }

    public event Action<StatusSnapshot>? DeviceStatusObserved;
    public event Action<DeviceConnectionSnapshot>? ConnectionObserved;
    public event Action<DeviceEventEnvelope>? DeviceEventReceived;

    public IReadOnlyCollection<string> GetCommands(string deviceId)
        => _inner?.GetCommands(deviceId) ?? Array.Empty<string>();

    public bool TryGetDevice(string deviceId, out DeviceDescriptor info)
    {
        if (_inner is not null)
            return _inner.TryGetDevice(deviceId, out info);

        var descriptor = _descriptors.FirstOrDefault(x => string.Equals(x.EffectiveId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (descriptor is not null)
        {
            info = descriptor;
            return true;
        }

        info = default!;
        return false;
    }

    public IReadOnlyList<DeviceDescriptor> GetAllDevices()
        => _inner?.GetAllDevices() ?? _descriptors.ToArray();

    public async Task<IReadOnlyList<StatusSnapshot>> GetStatusesAsync(CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).GetStatusesAsync(cancellationToken).ConfigureAwait(false);

    public async Task<StatusSnapshot?> GetStatusAsync(string deviceId, CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).GetStatusAsync(deviceId, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DeviceConnectionSnapshot>> GetConnectionsAsync(CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).GetConnectionsAsync(cancellationToken).ConfigureAwait(false);

    public async Task<DeviceConnectionSnapshot?> GetConnectionAsync(string deviceId, CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).GetConnectionAsync(deviceId, cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(DeviceDescriptor descriptor, CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).AddAsync(descriptor, cancellationToken).ConfigureAwait(false);

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).ConnectAsync(deviceId, cancellationToken).ConfigureAwait(false);

    public async Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).DisconnectAsync(deviceId, cancellationToken).ConfigureAwait(false);

    public async Task<DeviceCommandResponse> ExecuteAsync(string deviceId, DeviceCommandRequest command, CancellationToken cancellationToken = default)
        => await (await EnsureInnerAsync(cancellationToken).ConfigureAwait(false)).ExecuteAsync(deviceId, command, cancellationToken).ConfigureAwait(false);

    public async ValueTask DisposeAsync()
    {
        if (_inner is not null)
        {
            Unsubscribe(_inner);
            await _inner.DisposeAsync().ConfigureAwait(false);
            _inner = null;
        }

        _gate.Dispose();
    }

    private async Task<DeviceRuntimePort> EnsureInnerAsync(CancellationToken cancellationToken)
    {
        if (_inner is not null)
            return _inner;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_inner is not null)
                return _inner;

            _inner = new DeviceRuntimePort(_descriptors, _options);
            Subscribe(_inner);
            return _inner;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void Subscribe(DeviceRuntimePort inner)
    {
        inner.DeviceStatusObserved += OnStatusObserved;
        inner.ConnectionObserved += OnConnectionObserved;
        inner.DeviceEventReceived += OnDeviceEventReceived;
    }

    private void Unsubscribe(DeviceRuntimePort inner)
    {
        inner.DeviceStatusObserved -= OnStatusObserved;
        inner.ConnectionObserved -= OnConnectionObserved;
        inner.DeviceEventReceived -= OnDeviceEventReceived;
    }

    private void OnStatusObserved(StatusSnapshot snapshot) => DeviceStatusObserved?.Invoke(snapshot);
    private void OnConnectionObserved(DeviceConnectionSnapshot snapshot) => ConnectionObserved?.Invoke(snapshot);
    private void OnDeviceEventReceived(DeviceEventEnvelope envelope) => DeviceEventReceived?.Invoke(envelope);
}
