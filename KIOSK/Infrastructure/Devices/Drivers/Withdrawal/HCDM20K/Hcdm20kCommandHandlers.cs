using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Drivers.HCDM20K;

namespace KIOSK.Devices.Drivers.HCDM20K
{
    internal static class Hcdm20kCommandHandlers
    {
        public static IReadOnlyCollection<IDeviceCommandHandler> Create(Hcdm20kClient client, string deviceKey)
        {
            var unknown = CommandResultFactory.UnknownCommand(deviceKey);
            return new IDeviceCommandHandler[]
            {
                new RestartHandler(),
                new SensorHandler(client),
                new InitHandler(client, unknown),
                new VersionHandler(client),
                new EjectHandler(client, unknown),
                new DispenseHandler(client, unknown)
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
            private readonly Hcdm20kClient _client;
            public SensorHandler(Hcdm20kClient client) => _client = client;
            public string Name => "SENSOR";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.SendCommandAsync(Hcdm20kCommand.Sensor, Array.Empty<byte>(), processTimeoutMs: 2000, ct: ct);
        }

        private sealed class InitHandler : IDeviceCommandHandler
        {
            private readonly Hcdm20kClient _client;
            private readonly CommandResult _unknown;
            public InitHandler(Hcdm20kClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "INIT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is byte[] data
                    ? _client.SendCommandAsync(Hcdm20kCommand.Initialize, data, processTimeoutMs: 8000, ct: ct)
                    : Task.FromResult(_unknown);
        }

        private sealed class VersionHandler : IDeviceCommandHandler
        {
            private readonly Hcdm20kClient _client;
            public VersionHandler(Hcdm20kClient client) => _client = client;
            public string Name => "VERSION";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.SendCommandAsync(Hcdm20kCommand.Version, Array.Empty<byte>(), processTimeoutMs: 2000, ct: ct);
        }

        private sealed class EjectHandler : IDeviceCommandHandler
        {
            private readonly Hcdm20kClient _client;
            private readonly CommandResult _unknown;
            public EjectHandler(Hcdm20kClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "EJECT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            {
                if (command.Payload is not byte[] data)
                    return Task.FromResult(_unknown);

                var args = new[] { (data.Length > 0) ? Encoding.ASCII.GetString(data) : "0" };
                var payload = Encoding.ASCII.GetBytes(string.Concat(args));
                return _client.SendCommandAsync(Hcdm20kCommand.Eject, payload, processTimeoutMs: 5000, ct: ct);
            }
        }

        private sealed class DispenseHandler : IDeviceCommandHandler
        {
            private readonly Hcdm20kClient _client;
            private readonly CommandResult _unknown;
            public DispenseHandler(Hcdm20kClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "DISPENSE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            {
                if (command.Payload is not byte[] data)
                    return Task.FromResult(_unknown);

                int estimatedCount = EstimateTotalRequestedFromPayload(data);
                int timeoutMs = (int)((estimatedCount / 3.0 + 5) * 1000);
                return _client.SendCommandAsync(
                    Hcdm20kCommand.Dispense,
                    data,
                    processTimeoutMs: Math.Max(timeoutMs, 15000),
                    ct: ct,
                    isLongOpWithEnq: true);
            }
        }

        private static int EstimateTotalRequestedFromPayload(byte[] payload)
        {
            if (payload.Length == 0) return 0;
            try
            {
                string s = Encoding.ASCII.GetString(payload);
                if (s.Length == 0) return 0;

                int i = 0;
                int total = 0;

                if (i < s.Length && char.IsDigit(s[i]))
                {
                    int n = s[i] - '0';
                    i++;
                    for (int k = 0; k < n; k++)
                    {
                        if (i + 4 <= s.Length)
                        {
                            i += 1;
                            if (int.TryParse(s.AsSpan(i, Math.Min(3, s.Length - i)), out int c))
                                total += c;
                            i += 3;
                        }
                    }
                }
                return total;
            }
            catch { return 0; }
        }
    }
}
