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
        private readonly IStatusPipeline _statusPipeline;
        private readonly IDeviceCommandCatalog _commandCatalog;
        private readonly IErrorMessageProvider _messages;
        private readonly DeviceCommandLogRepository _commandRepository;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            IDeviceHost host,
            IStatusPipeline statusPipeline,
            IDeviceCommandCatalog commandCatalog,
            IErrorMessageProvider messages,
            DeviceCommandLogRepository commandRepository,
            ILogger<DeviceService> logger)
        {
            _host = host;
            _statusPipeline = statusPipeline;
            _commandCatalog = commandCatalog;
            _messages = messages;
            _commandRepository = commandRepository;
            _logger = logger;

            _host.StatusUpdated += (name, snap) => _statusPipeline.Process(name, snap);
        }

        public Task AddAsync(DeviceDescriptor desc, CancellationToken ct = default)
        {
            if (desc is null || !desc.Validate)
                return Task.CompletedTask;

            _statusPipeline.Process(desc.Name, new StatusSnapshot
            {
                Name = desc.Name,
                Model = desc.Model,
                Health = DeviceHealth.Offline,
                Timestamp = DateTimeOffset.UtcNow
            });
            return _host.AddAsync(desc, ct);
        }

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
