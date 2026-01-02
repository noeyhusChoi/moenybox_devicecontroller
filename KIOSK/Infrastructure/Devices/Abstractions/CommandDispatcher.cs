using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Device.Abstractions
{
    public interface IDeviceCommandHandler
    {
        string Name { get; }
        Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct);
    }

    public sealed class CommandDispatcher
    {
        private readonly IReadOnlyDictionary<string, IDeviceCommandHandler> _handlers;
        private readonly Func<CommandResult> _unknownResultFactory;

        public CommandDispatcher(
            IEnumerable<IDeviceCommandHandler> handlers,
            Func<CommandResult> unknownResultFactory)
        {
            _handlers = handlers.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
            _unknownResultFactory = unknownResultFactory ?? throw new ArgumentNullException(nameof(unknownResultFactory));
        }

        public Task<CommandResult> DispatchAsync(DeviceCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                return Task.FromResult(_unknownResultFactory());

            if (!_handlers.TryGetValue(command.Name, out var handler))
                return Task.FromResult(_unknownResultFactory());

            return handler.HandleAsync(command, ct);
        }
    }
}
