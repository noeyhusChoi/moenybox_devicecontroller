using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Drivers.HCDM;

namespace KIOSK.Devices.Drivers.HCDM
{
    internal static class Hcdm10kCommandHandlers
    {
        public static IReadOnlyCollection<IDeviceCommandHandler> Create(Hcdm10kClient client, string deviceKey)
        {
            var unknown = CommandResultFactory.UnknownCommand(deviceKey);
            return new IDeviceCommandHandler[]
            {
                new RestartHandler(),
                new SensorHandler(client, unknown),
                new InitHandler(client, unknown),
                new DispenseHandler(client, unknown),
                new EjectHandler(client, unknown)
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
            private readonly CommandResult _unknown;
            public SensorHandler(Hcdm10kClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "SENSOR";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[]
                    ? _client.SendCommandAsync(Hcdm10kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 5000, ct: ct)
                    : Task.FromResult(_unknown);
        }

        private sealed class InitHandler : IDeviceCommandHandler
        {
            private readonly Hcdm10kClient _client;
            private readonly CommandResult _unknown;
            public InitHandler(Hcdm10kClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "INIT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[]
                    ? _client.SendCommandAsync(Hcdm10kCommand.Initialize, new byte[] { 0x00 }, processTimeoutMs: 30000, ct: ct)
                    : Task.FromResult(_unknown);
        }

        private sealed class DispenseHandler : IDeviceCommandHandler
        {
            private readonly Hcdm10kClient _client;
            private readonly CommandResult _unknown;
            public DispenseHandler(Hcdm10kClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "DISPENSE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[] data
                    ? _client.SendCommandAsync(Hcdm10kCommand.Dispense, data, processTimeoutMs: 120000, ct: ct)
                    : Task.FromResult(_unknown);
        }

        private sealed class EjectHandler : IDeviceCommandHandler
        {
            private readonly Hcdm10kClient _client;
            private readonly CommandResult _unknown;
            public EjectHandler(Hcdm10kClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "EJECT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[] data
                    ? _client.SendCommandAsync(Hcdm10kCommand.Eject, data, processTimeoutMs: 10000, ct: ct)
                    : Task.FromResult(_unknown);
        }
    }
}
