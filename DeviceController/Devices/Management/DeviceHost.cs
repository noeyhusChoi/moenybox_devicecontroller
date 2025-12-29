using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Status;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KIOSK.Devices.Management
{
    public interface IDeviceHost : IAsyncDisposable
    {
        Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default);

        event Action<string>? Connected;
        event Action<string>? Disconnected;
        event Action<string, StatusSnapshot>? StatusUpdated;
        event Action<string, Exception>? Faulted;

        bool TryGetSupervisor(string name, out DeviceSupervisor sup);
        IEnumerable<DeviceSupervisor> GetAllSupervisors();

        Task<CommandResult> ExecuteAsync(string name, DeviceCommand cmd, CommandContext context, CancellationToken ct = default);
    }

    public sealed class DeviceHost : IDeviceHost
    {
        private readonly ITransportFactory _transportFactory;
        private readonly IDeviceFactory _deviceFactory;
        private readonly ConcurrentDictionary<string, DeviceSupervisor> _supers = new();
        private readonly CancellationTokenSource _cts = new();

        public DeviceHost(
            ITransportFactory transportFactory,
            IDeviceFactory deviceFactory)
        {
            _transportFactory = transportFactory;
            _deviceFactory = deviceFactory;
        }

        public event Action<string, StatusSnapshot>? StatusUpdated;
        public event Action<string>? Connected;
        public event Action<string>? Disconnected;
        public event Action<string, Exception>? Faulted;

        public Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default)
        {
            if (desc == null || !desc.Validate)
                return Task.CompletedTask;

            var sup = new DeviceSupervisor(desc, _transportFactory, _deviceFactory);

            // 상태 이벤트 전달
            sup.StatusUpdated += (id, snap) => StatusUpdated?.Invoke(id, snap);
            sup.Connected += id => Connected?.Invoke(id);
            sup.Disconnected += id => Disconnected?.Invoke(id);
            sup.Faulted += (id, ex) => Faulted?.Invoke(id, ex);

            // Supervisor 등록
            if (!_supers.TryAdd(desc.Name, sup))
                throw new InvalidOperationException($"Duplicated device name: {desc.Name}");

            // Supervisor 실행
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            _ = sup.RunAsync(linkedCts.Token).ContinueWith(_ => linkedCts.Dispose());

            return Task.CompletedTask;
        }

        public async Task<CommandResult> ExecuteAsync(string name, DeviceCommand cmd, CommandContext context, CancellationToken ct = default)
        {
            if (!_supers.TryGetValue(name, out var sup))
            {
                return new CommandResult(false, string.Empty, Code: new ErrorCode("SYS", "APP", "CONFIG", "INVALID"));
            }

            var result = await sup.ExecuteAsync(cmd, ct).ConfigureAwait(false);
            return result;
        }

        public bool TryGetSupervisor(string name, out DeviceSupervisor sup)
            => _supers.TryGetValue(name, out sup);

        public IEnumerable<DeviceSupervisor> GetAllSupervisors()
            => _supers.Values;

        public async ValueTask DisposeAsync()
        {
            try { _cts.Cancel(); } catch (ObjectDisposedException) { }
            foreach (var sup in _supers.Values)
                await sup.DisposeAsync();

            _cts.Dispose();
        }

    }
}
