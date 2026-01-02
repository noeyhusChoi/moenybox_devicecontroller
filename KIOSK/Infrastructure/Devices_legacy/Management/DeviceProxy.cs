using KIOSK.Device.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Devices.Management
{
    /// <summary>
    /// IDevice 인스턴스 교체를 외부에 투명하게 만드는 프록시.
    /// Supervisor가 재연결 시 새 디바이스를 생성해도 외부 핸들은 동일 객체를 유지한다.
    /// </summary>
    public sealed class DeviceProxy : IDevice, IAsyncDisposable
    {
        private readonly object _lock = new();
        private IDevice _inner;

        public DeviceProxy(IDevice initial)
        {
            _inner = initial ?? throw new ArgumentNullException(nameof(initial));
        }

        public string Name
        {
            get { lock (_lock) return _inner.Name; }
        }

        public string Model
        {
            get { lock (_lock) return _inner.Model; }
        }

        public void Swap(IDevice next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            lock (_lock) { _inner = next; }
        }

        public Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
        {
            IDevice snapshot;
            lock (_lock) snapshot = _inner;
            return snapshot.InitializeAsync(ct);
        }

        public Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default, string snapshotId = "")
        {
            IDevice snapshot;
            lock (_lock) snapshot = _inner;
            return snapshot.GetStatusAsync(ct, snapshotId);
        }

        public Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            IDevice snapshot;
            lock (_lock) snapshot = _inner;
            return snapshot.ExecuteAsync(command, ct);
        }

        public T? GetInner<T>() where T : class, IDevice
        {
            lock (_lock) return _inner as T;
        }

        public ValueTask DisposeAsync()
        {
            IDevice snapshot;
            lock (_lock) snapshot = _inner;

            if (snapshot is IAsyncDisposable asyncDisposable)
                return asyncDisposable.DisposeAsync();

            if (snapshot is IDisposable disposable)
                disposable.Dispose();

            return ValueTask.CompletedTask;
        }
    }
}
