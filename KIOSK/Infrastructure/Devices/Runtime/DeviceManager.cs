using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime.Factories;
using KIOSK.Infrastructure.Devices.Status;
using KIOSK.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Devices.Runtime
{
    public interface IDeviceManager : IAsyncDisposable
    {
        Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default);
        Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default);
        Task<bool> DisconnectAsync(string deviceId, CancellationToken ct = default);

        event Action<string, StatusSnapshot>? StatusUpdated;
        event Action<string>? Connected;
        event Action<string>? Disconnected;

        // 명령
        Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CancellationToken ct = default);
        Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CommandContext context, CancellationToken ct = default);

        bool TryGetInnerDevice<TDevice>(string deviceId, out TDevice device) where TDevice : class, IDeviceDriver;
        bool TryGetDevice(string deviceId, out DeviceRuntimeInfo info);
        IReadOnlyList<DeviceRuntimeInfo> GetAllDevices();
    }

    public sealed class DeviceManager : IDeviceManager
    {
        private readonly ITransportFactory _transportFactory;
        private readonly IDeviceDriverFactory _deviceFactory;
        private readonly ILogger<DeviceSupervisor> _supervisorLogger;
        private readonly IStatusPipeline _statusPipeline;
        private readonly IErrorMessageProvider _messages;
        private readonly DeviceCommandLogRepository _commandRepository;
        private readonly ILogger<DeviceManager> _logger;
        private readonly ConcurrentDictionary<string, DeviceSupervisor> _supers = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Dictionary<string, string> _displayNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DeviceRuntimeInfo> _devices = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _devicesSync = new();

        public DeviceManager(
            ITransportFactory transportFactory,
            IDeviceDriverFactory deviceFactory,
            ILoggerFactory loggerFactory,
            IStatusPipeline statusPipeline,
            IErrorMessageProvider messages,
            DeviceCommandLogRepository commandRepository,
            ILogger<DeviceManager> logger)
        {
            _transportFactory = transportFactory;
            _deviceFactory = deviceFactory;
            _supervisorLogger = loggerFactory.CreateLogger<DeviceSupervisor>();
            _statusPipeline = statusPipeline;
            _messages = messages;
            _commandRepository = commandRepository;
            _logger = logger;
        }

        public event Action<string, StatusSnapshot>? StatusUpdated;
        public event Action<string>? Connected;
        public event Action<string>? Disconnected;

        public Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default)
        {
            if (desc is null || !desc.Validate)
                return Task.CompletedTask;

            var sup = new DeviceSupervisor(desc, _transportFactory, _deviceFactory, _supervisorLogger);
            sup.StatusUpdated += OnSupervisorStatusUpdated;
            sup.Connected += OnSupervisorConnected;
            sup.Disconnected += OnSupervisorDisconnected;

            var deviceId = desc.EffectiveId;
            if (!_supers.TryAdd(deviceId, sup))
                throw new InvalidOperationException($"Duplicated device id: {deviceId}");

            _displayNames[desc.EffectiveId] = desc.Name;
            lock (_devicesSync)
            {
                _devices[desc.EffectiveId] = new DeviceRuntimeInfo(
                    desc.EffectiveId,
                    desc.Name,
                    desc.Vendor,
                    desc.Model,
                    desc.TransportType,
                    desc.TransportPort,
                    desc.TransportParam,
                    desc.ProtocolName,
                    desc.PollingMs,
                    desc.DeviceType,
                    desc.Driver);
            }

            _statusPipeline.Process(desc.EffectiveId, new StatusSnapshot
            {
                Name = desc.Name,
                Model = desc.Model,
                Health = DeviceHealth.Offline,
                Timestamp = DateTimeOffset.UtcNow
            });

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            _ = sup.RunAsync(linkedCts.Token).ContinueWith(_ => linkedCts.Dispose());

            return Task.CompletedTask;
        }

        public Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CancellationToken ct = default)
            => SendAsync(name, cmd, CommandContext.Auto(), ct);

        public Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_supers.TryGetValue(deviceId, out var sup))
                return Task.FromResult(false);

            return sup.ConnectAsync(ct);
        }

        public Task<bool> DisconnectAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_supers.TryGetValue(deviceId, out var sup))
                return Task.FromResult(false);

            return sup.DisconnectAsync(ct);
        }

        public async Task<CommandResult> SendAsync(string name, DeviceCommand cmd, CommandContext context, CancellationToken ct = default)
        {
            var startedAt = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();

            var result = await ExecuteAsync(name, cmd, context, ct).ConfigureAwait(false);
            if (result.Code is { } code)
            {
                var message = _messages.GetMessage(code) ?? string.Empty;
                result = result with { Message = message };
            }

            _logger.LogInformation(
                "[Command] {Device} {Command} success={Success} code={Code} durationMs={DurationMs}",
                GetDisplayName(name),
                cmd.Name,
                result.Success,
                result.Code?.ToString(),
                sw.ElapsedMilliseconds);

            PublishCommandRecord(name, cmd, context, result, startedAt, sw.ElapsedMilliseconds);
            return result;
        }

        public bool TryGetInnerDevice<TDevice>(string deviceId, out TDevice device) where TDevice : class, IDeviceDriver
        {
            device = default!;
            if (!_supers.TryGetValue(deviceId, out var sup))
                return false;

            device = sup.GetInnerDevice<TDevice>();
            return device != null;
        }

        public bool TryGetDevice(string deviceId, out DeviceRuntimeInfo info)
        {
            lock (_devicesSync)
            {
                return _devices.TryGetValue(deviceId, out info!);
            }
        }

        public IReadOnlyList<DeviceRuntimeInfo> GetAllDevices()
        {
            lock (_devicesSync)
            {
                return _devices.Values.ToList();
            }
        }

        public ValueTask DisposeAsync()
        {
            try { _cts.Cancel(); } catch (ObjectDisposedException) { }
            return DisposeCoreAsync();
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
                GetDisplayName(name),
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

        private string GetDisplayName(string deviceId)
            => _displayNames.TryGetValue(deviceId, out var name) ? name : deviceId;

        private Task<CommandResult> ExecuteAsync(string deviceId, DeviceCommand cmd, CommandContext context, CancellationToken ct = default)
        {
            if (!_supers.TryGetValue(deviceId, out var sup))
                return Task.FromResult(new CommandResult(false, string.Empty, Code: new ErrorCode("SYS", "APP", "CONFIG", "INVALID")));

            return sup.ExecuteAsync(cmd, ct);
        }

        private void OnSupervisorStatusUpdated(string deviceId, StatusSnapshot snapshot)
        {
            _statusPipeline.Process(deviceId, snapshot);
            StatusUpdated?.Invoke(deviceId, snapshot);
        }

        private void OnSupervisorConnected(string deviceId)
            => Connected?.Invoke(deviceId);

        private void OnSupervisorDisconnected(string deviceId)
            => Disconnected?.Invoke(deviceId);

        private async ValueTask DisposeCoreAsync()
        {
            foreach (var sup in _supers.Values)
                await sup.DisposeAsync();

            _cts.Dispose();
        }

    }
}
