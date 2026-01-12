using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.EM20;

namespace KIOSK.Device.Drivers.EM20
{
    internal static class Em20CommandHandlers
    {
        public static IReadOnlyCollection<IDeviceCommandHandler> Create(Em20Client client)
            => new IDeviceCommandHandler[]
            {
                new RestartHandler(),
                new ScanOnceHandler(client),
                new ScanManyHandler(client),
                new TriggerOnHandler(client),
                new TriggerOffHandler(client),
                new ReadHandler(client)
            };

        private sealed class RestartHandler : IDeviceCommandHandler
        {
            public string Name => "RESTART";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => Task.FromResult(new CommandResult(true));
        }

        private sealed class ScanOnceHandler : IDeviceCommandHandler
        {
            private readonly Em20Client _client;
            public ScanOnceHandler(Em20Client client) => _client = client;
            public string Name => "SCAN.ONCE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.ScanOnceAsync(ct);
        }

        private sealed class ScanManyHandler : IDeviceCommandHandler
        {
            private readonly Em20Client _client;
            public ScanManyHandler(Em20Client client) => _client = client;
            public string Name => "SCAN.MANY";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.ScanManyAsync(count: 3, ct);
        }

        private sealed class TriggerOnHandler : IDeviceCommandHandler
        {
            private readonly Em20Client _client;
            public TriggerOnHandler(Em20Client client) => _client = client;
            public string Name => "SCAN.TRIGGERON";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.TriggerAsync(true, ct);
        }

        private sealed class TriggerOffHandler : IDeviceCommandHandler
        {
            private readonly Em20Client _client;
            public TriggerOffHandler(Em20Client client) => _client = client;
            public string Name => "SCAN.TRIGGEROFF";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.TriggerAsync(false, ct);
        }

        private sealed class ReadHandler : IDeviceCommandHandler
        {
            private readonly Em20Client _client;
            public ReadHandler(Em20Client client) => _client = client;
            public string Name => "SCAN.READ";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.ReadRawAsync(timeoutMs: 1000, ct);
        }
    }
}
