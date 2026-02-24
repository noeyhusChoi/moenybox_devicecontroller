using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.HCDM;

namespace KIOSK.Device.Drivers.HCDM
{
    internal static class Hcdm10kCommandHandlers
    {
        public static IReadOnlyCollection<DeviceCommandDescriptor> SupportedCommands { get; } =
            new[]
            {
                new DeviceCommandDescriptor("RESTART", "재시작"),
                new DeviceCommandDescriptor("SENSOR", "센서 조회"),
                new DeviceCommandDescriptor("INIT", "초기화"),
                new DeviceCommandDescriptor("DISPENSE", "지폐 방출"),
                new DeviceCommandDescriptor("EJECT", "방출/회수"),
            };

        public static IReadOnlyCollection<IDeviceCommandHandler> Create(Hcdm10kClient client, string deviceKey)
        {
            var invalidPayload = CommandResults.InvalidPayload(deviceKey);
            return new IDeviceCommandHandler[]
            {
                new RestartHandler(),
                new SensorHandler(client, invalidPayload),
                new InitHandler(client, invalidPayload),
                new DispenseHandler(client, invalidPayload),
                new EjectHandler(client, invalidPayload)
            };
        }

        private sealed class RestartHandler : IDeviceCommandHandler
        {
            public string Name => "RESTART";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => Task.FromResult(new CommandResult(true));
        }

        private sealed class SensorHandler : IDeviceCommandHandler
        {
            private readonly Hcdm10kClient _client;
            private readonly CommandResult _invalidPayload;
            public SensorHandler(Hcdm10kClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "SENSOR";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[]
                    ? _client.SendCommandAsync(Hcdm10kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 5000, ct: ct)
                    : Task.FromResult(_invalidPayload);
        }

        private sealed class InitHandler : IDeviceCommandHandler
        {
            private readonly Hcdm10kClient _client;
            private readonly CommandResult _invalidPayload;
            public InitHandler(Hcdm10kClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "INIT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[]
                    ? _client.SendCommandAsync(Hcdm10kCommand.Initialize, new byte[] { 0x00 }, processTimeoutMs: 30000, ct: ct)
                    : Task.FromResult(_invalidPayload);
        }

        private sealed class DispenseHandler : IDeviceCommandHandler
        {
            private readonly Hcdm10kClient _client;
            private readonly CommandResult _invalidPayload;
            public DispenseHandler(Hcdm10kClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "DISPENSE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[] data
                    ? _client.SendCommandAsync(Hcdm10kCommand.Dispense, data, processTimeoutMs: 120000, ct: ct)
                    : Task.FromResult(_invalidPayload);
        }

        private sealed class EjectHandler : IDeviceCommandHandler
        {
            private readonly Hcdm10kClient _client;
            private readonly CommandResult _invalidPayload;
            public EjectHandler(Hcdm10kClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "EJECT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[] data
                    ? _client.SendCommandAsync(Hcdm10kCommand.Eject, data, processTimeoutMs: 10000, ct: ct)
                    : Task.FromResult(_invalidPayload);
        }
    }
}
