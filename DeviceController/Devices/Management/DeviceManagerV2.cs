using KIOSK.Device.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Devices.Management
{
    public interface IDeviceManager : IAsyncDisposable
    {
        Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default);

        // 상태
        event Action<string, DeviceStatusSnapshot>? StatusUpdated;
        IReadOnlyCollection<DeviceStatusSnapshot> GetLatestSnapshots();

        // 명령
        Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CancellationToken ct = default);
        IReadOnlyCollection<DeviceCommandDescriptor> GetCommands(string name);
        IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAllCommands();

        // 필요하면 헬퍼
        T? GetDevice<T>(string name) where T : class, IDevice;
    }

    public sealed class DeviceManagerV2 : IDeviceManager
    {
        private readonly IDeviceRuntime _runtime;
        private readonly IDeviceStatusStore _statusStore;
        private readonly IDeviceCommandCatalog _commandCatalog;

        public DeviceManagerV2(
            IDeviceRuntime runtime,
            IDeviceStatusStore statusStore,
            IDeviceCommandCatalog commandCatalog)
        {
            _runtime = runtime;
            _statusStore = statusStore;
            _commandCatalog = commandCatalog;

            // DeviceStatusStore의 StatusUpdated를 그대로 re-publish
            _statusStore.StatusUpdated += (name, snap) =>
            {
                try { StatusUpdated?.Invoke(name, snap); } 
                catch (Exception ex) { Trace.WriteLine(ex); }
            };
        }

        public Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default)
            => _runtime.AddAsync(desc, ct);

        public event Action<string, DeviceStatusSnapshot>? StatusUpdated;

        public IReadOnlyCollection<DeviceStatusSnapshot> GetLatestSnapshots()
            => _statusStore.GetAll();

        public Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CancellationToken ct = default)
            => _runtime.ExecuteAsync(name, cmd, ct);

        public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands(string name)
            => _commandCatalog.GetFor(name);

        public IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAllCommands()
            => _commandCatalog.GetAll();

        public T? GetDevice<T>(string name) where T : class, IDevice
        {
            if (_runtime.TryGetSupervisor(name, out var sup))
                return sup.GetInnerDevice<T>();

            return null;
        }

        public ValueTask DisposeAsync()
            => _runtime.DisposeAsync();

    }
}
