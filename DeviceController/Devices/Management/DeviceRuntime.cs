using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Device.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Devices.Management
{
    public interface IDeviceRuntime : IAsyncDisposable
    {
        Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default);

        bool TryGetSupervisor(string name, out DeviceSupervisorV2 sup);
        IEnumerable<DeviceSupervisorV2> GetAllSupervisors();
    }

    public sealed class DeviceRuntime : IDeviceRuntime
    {
        private readonly IDeviceStatusStore _statusStore;
        private readonly ITransportFactory _transportFactory;
        private readonly IDeviceFactory _deviceFactory;
        private readonly ConcurrentDictionary<string, DeviceSupervisorV2> _supers = new();
        private readonly CancellationTokenSource _cts = new();

        public DeviceRuntime(
            IDeviceStatusStore statusStore,
            ITransportFactory transportFactory,
            IDeviceFactory deviceFactory)
        {
            _statusStore = statusStore;
            _transportFactory = transportFactory;
            _deviceFactory = deviceFactory;
        }

        public Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default)
        {
            if (desc == null || !desc.Validate)
                return Task.CompletedTask;

            var sup = new DeviceSupervisorV2(desc, _transportFactory, _deviceFactory);

            // 초기 Offline 스냅샷 생성
            _statusStore.Initialize(desc);

            // SupervisorV2 상태를 Store에 반영
            sup.StatusUpdated += (id, snap) => { _statusStore.Update(id, snap); };
            sup.Connected += HandleConnected;
            sup.Disconnected += HandleDisconnected;
            sup.Faulted += HandleFaulted;

            // Supervisor 등록
            if (!_supers.TryAdd(desc.Name, sup))
                throw new InvalidOperationException($"Duplicated device name: {desc.Name}");

            // Supervisor 실행
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            _ = sup.RunAsync(linkedCts.Token).ContinueWith(_ => linkedCts.Dispose());

            return Task.CompletedTask;
        }

        public bool TryGetSupervisor(string name, out DeviceSupervisorV2 sup)
            => _supers.TryGetValue(name, out sup);

        public IEnumerable<DeviceSupervisorV2> GetAllSupervisors()
            => _supers.Values;

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            foreach (var sup in _supers.Values)
                await sup.DisposeAsync();

            _cts.Dispose();
        }

        private void HandleConnected(string name)
        {
            var prev = _statusStore.TryGet(name);
            _statusStore.Update(name, new DeviceStatusSnapshot
            {
                Name = name,
                Model = ResolveModel(name),
                Health = DeviceHealth.Online,
                Alarms = prev?.Alarms ?? new(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        private void HandleDisconnected(string name)
        {
            var prev = _statusStore.TryGet(name);
            _statusStore.Update(name, new DeviceStatusSnapshot
            {
                Name = name,
                Model = ResolveModel(name),
                Health = DeviceHealth.Offline,
                Alarms = prev?.Alarms ?? new(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        private void HandleFaulted(string name, Exception ex)
        {
            var prev = _statusStore.TryGet(name);
            var alarms = prev?.Alarms?.ToList() ?? new List<DeviceAlarm>();
            alarms.Add(new DeviceAlarm("FAULT", ex.Message, Severity.Error, DateTimeOffset.UtcNow));

            _statusStore.Update(name, new DeviceStatusSnapshot
            {
                Name = name,
                Model = ResolveModel(name),
                Health = DeviceHealth.Offline,
                Alarms = alarms,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        private string ResolveModel(string name)
        {
            if (_supers.TryGetValue(name, out var sup))
                return sup.Model;

            var snap = _statusStore.TryGet(name);
            return snap?.Model ?? string.Empty;
        }
    }
}
