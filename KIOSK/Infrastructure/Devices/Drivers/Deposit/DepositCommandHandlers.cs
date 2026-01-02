using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.Deposit
{
    internal static class DepositCommandHandlers
    {
        public static IReadOnlyCollection<IDeviceCommandHandler> Create(DepositClient client)
            => new IDeviceCommandHandler[]
            {
                new RestartHandler(),
                new StartHandler(client),
                new StopHandler(client),
                new StackHandler(client),
                new ReturnHandler(client)
            };

        private sealed class RestartHandler : IDeviceCommandHandler
        {
            public string Name => "RESTART";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => Task.FromResult(new CommandResult(true));
        }

        private sealed class StartHandler : IDeviceCommandHandler
        {
            private readonly DepositClient _client;
            public StartHandler(DepositClient client) => _client = client;
            public string Name => "START";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.StartAcceptanceAsync();
        }

        private sealed class StopHandler : IDeviceCommandHandler
        {
            private readonly DepositClient _client;
            public StopHandler(DepositClient client) => _client = client;
            public string Name => "STOP";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.StopAcceptanceAsync();
        }

        private sealed class StackHandler : IDeviceCommandHandler
        {
            private readonly DepositClient _client;
            public StackHandler(DepositClient client) => _client = client;
            public string Name => "STACK";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.StackAsync(ct);
        }

        private sealed class ReturnHandler : IDeviceCommandHandler
        {
            private readonly DepositClient _client;
            public ReturnHandler(DepositClient client) => _client = client;
            public string Name => "RETURN";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.ReturnAsync(ct);
        }
    }
}
