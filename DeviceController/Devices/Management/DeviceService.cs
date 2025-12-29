using KIOSK.Device.Abstractions;
using KIOSK.Status;
using KIOSK.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KIOSK.Devices.Management
{
    public interface IDeviceManager : IAsyncDisposable
    {
        Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default);

        // 상태
        event Action<string, StatusSnapshot>? StatusUpdated;
        IReadOnlyCollection<StatusSnapshot> GetLatestSnapshots();

        // 명령
        Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CancellationToken ct = default);
        Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CommandContext context, CancellationToken ct = default);
        IReadOnlyCollection<DeviceCommandDescriptor> GetCommands(string name);
        IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAllCommands();

        // 필요하면 헬퍼
        T? GetDevice<T>(string name) where T : class, IDevice;
    }

    public sealed class DeviceService : IDeviceManager
    {
        private readonly IDeviceHost _host;
        private readonly IStatusStore _statusStore;
        private readonly IStatusPipeline _statusPipeline;
        private readonly IDeviceCommandCatalog _commandCatalog;
        private readonly IErrorMessageProvider _messages;
        private readonly DeviceCommandLogRepository _commandRepository;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            IDeviceHost host,
            IStatusStore statusStore,
            IStatusPipeline statusPipeline,
            IDeviceCommandCatalog commandCatalog,
            IErrorMessageProvider messages,
            DeviceCommandLogRepository commandRepository,
            ILogger<DeviceService> logger)
        {
            _host = host;
            _statusStore = statusStore;
            _statusPipeline = statusPipeline;
            _commandCatalog = commandCatalog;
            _messages = messages;
            _commandRepository = commandRepository;
            _logger = logger;

            _host.StatusUpdated += (name, snap) => _statusPipeline.Process(name, snap);
            _host.Connected += HandleConnected;
            _host.Disconnected += HandleDisconnected;
            _host.Faulted += HandleFaulted;
        }

        public Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default)
        {
            if (desc is null || !desc.Validate)
                return Task.CompletedTask;

            _statusStore.Initialize(desc);
            return _host.AddAsync(desc, ct);
        }

        public event Action<string, StatusSnapshot>? StatusUpdated
        {
            add => _statusStore.StatusUpdated += value;
            remove => _statusStore.StatusUpdated -= value;
        }

        public IReadOnlyCollection<StatusSnapshot> GetLatestSnapshots()
            => _statusStore.GetAll();

        public Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CancellationToken ct = default)
            => SendAsync(name, cmd, CommandContext.Auto(), ct);

        public async Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CommandContext context, CancellationToken ct = default)
        {
            var startedAt = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();

            var result = await _host.ExecuteAsync(name, cmd, context, ct).ConfigureAwait(false);
            if (result.Code is { } code)
            {
                var message = _messages.GetMessage(code) ?? string.Empty;
                result = result with { Message = message };
            }

            _logger.LogInformation(
                "[Command] {Device} {Command} success={Success} code={Code} durationMs={DurationMs}",
                name,
                cmd.Name,
                result.Success,
                result.Code?.ToString(),
                sw.ElapsedMilliseconds);

            PublishCommandRecord(name, cmd, context, result, startedAt, sw.ElapsedMilliseconds);
            return result;
        }

        public IReadOnlyCollection<DeviceCommandDescriptor> GetCommands(string name)
            => _commandCatalog.GetFor(name);

        public IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAllCommands()
            => _commandCatalog.GetAll();

        public T? GetDevice<T>(string name) where T : class, IDevice
        {
            if (_host.TryGetSupervisor(name, out var sup))
                return sup.GetInnerDevice<T>();

            return null;
        }

        public ValueTask DisposeAsync()
            => _host.DisposeAsync();

        private void HandleConnected(string name)
        {
            var prev = _statusStore.TryGet(name);
            _statusPipeline.Process(name, new StatusSnapshot
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
            _statusPipeline.Process(name, new StatusSnapshot
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
            var alarms = prev?.Alarms?.ToList() ?? new List<StatusEvent>();
            var now = DateTimeOffset.UtcNow;
            var faultCode = new ErrorCode("SYS", "APP", "INTERNAL", "FAULT");
            var existingIndex = alarms.FindIndex(a =>
                string.Equals(a.Code, faultCode.ToString(), StringComparison.OrdinalIgnoreCase) &&
                a.Severity == Severity.Error);

            if (existingIndex < 0)
                alarms.Add(new StatusEvent(faultCode.ToString(), string.Empty, Severity.Error, now, ErrorCode: faultCode));

            _statusPipeline.Process(name, new StatusSnapshot
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
            if (_host.TryGetSupervisor(name, out var sup))
                return sup.Model;

            var snap = _statusStore.TryGet(name);
            return snap?.Model ?? string.Empty;
        }

        private void PublishCommandRecord(
            string name,
            DeviceCommand cmd,
            CommandContext context,
            CommandResult result,
            DateTimeOffset startedAt,
            long elapsedMs)
        {
            var finishedAt = DateTimeOffset.UtcNow;
            var record = new DeviceCommandRecord(
                name,
                cmd.Name,
                result.Success,
                result.Code,
                context.Origin,
                startedAt,
                finishedAt,
                elapsedMs);

            _ = Task.Run(async () =>
            {
                try { await _commandRepository.SaveAsync(record).ConfigureAwait(false); }
                catch { }
            });
        }

    }
}
