using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.E200Z
{
    internal static class E200ZCommandHandlers
    {
        public static IReadOnlyCollection<IDeviceCommandHandler> Create(
            E200ZClient client,
            Func<CancellationToken, Task<CommandResult>> requestRevision)
            => new IDeviceCommandHandler[]
            {
                new ScanEnableHandler(client),
                new ScanDisableHandler(client),
                new StartDecodeHandler(client),
                new StopDecodeHandler(client),
                new ResetHandler(client),
                new SetHostTriggerHandler(client),
                new SetAutoTriggerHandler(client),
                new SetPacketModeHandler(client),
                new RequestRevisionHandler(requestRevision),
                new RestartHandler()
            };

        private sealed class ScanEnableHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public ScanEnableHandler(E200ZClient client) => _client = client;
            public string Name => "SCAN_ENABLE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.ScanEnableAsync(ct);
        }

        private sealed class ScanDisableHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public ScanDisableHandler(E200ZClient client) => _client = client;
            public string Name => "SCAN_DISABLE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.ScanDisableAsync(ct);
        }

        private sealed class StartDecodeHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public StartDecodeHandler(E200ZClient client) => _client = client;
            public string Name => "START_DECODE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.StartDecodeAsync(ct);
        }

        private sealed class StopDecodeHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public StopDecodeHandler(E200ZClient client) => _client = client;
            public string Name => "STOP_DECODE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.StopDecodeAsync(ct);
        }

        private sealed class ResetHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public ResetHandler(E200ZClient client) => _client = client;
            public string Name => "RESET";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.ResetAsync(ct);
        }

        private sealed class SetHostTriggerHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public SetHostTriggerHandler(E200ZClient client) => _client = client;
            public string Name => "SET_HOST_TRIGGER";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.SetHostTriggerModeAsync(true, ct);
        }

        private sealed class SetAutoTriggerHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public SetAutoTriggerHandler(E200ZClient client) => _client = client;
            public string Name => "SET_AUTO_TRIGGER";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.SetAutoInductionTriggerModeAsync(true, ct);
        }

        private sealed class SetPacketModeHandler : IDeviceCommandHandler
        {
            private readonly E200ZClient _client;
            public SetPacketModeHandler(E200ZClient client) => _client = client;
            public string Name => "SET_PACKET_MODE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.SetDecodeDataPacketFormatAsync(0x01, true, ct);
        }

        private sealed class RequestRevisionHandler : IDeviceCommandHandler
        {
            private readonly Func<CancellationToken, Task<CommandResult>> _requestRevision;
            public RequestRevisionHandler(Func<CancellationToken, Task<CommandResult>> requestRevision)
                => _requestRevision = requestRevision;
            public string Name => "REQUEST_REVISION";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _requestRevision(ct);
        }

        private sealed class RestartHandler : IDeviceCommandHandler
        {
            public string Name => "RESTART";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => Task.FromResult(new CommandResult(true));
        }
    }
}
