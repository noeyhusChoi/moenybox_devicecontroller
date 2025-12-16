using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.Devices.Simulated
{
    public class SimulatedProtocol : IDeviceProtocol<SimulatedCommandId>
    {
        private readonly List<DeviceCommandMetadata> _commands = new()
        {
            new DeviceCommandMetadata(SimulatedCommandId.QueryStatus, "Query Status", "Polls device health/status.", true),
            new DeviceCommandMetadata(SimulatedCommandId.Ping, "Ping", "Connectivity check.", false),
            new DeviceCommandMetadata(SimulatedCommandId.Start, "Start", "Start the simulated job.", false),
            new DeviceCommandMetadata(SimulatedCommandId.Stop, "Stop", "Stop the simulated job.", false),
            new DeviceCommandMetadata(SimulatedCommandId.SetRate, "Set Rate", "Update target rate (Hz).", false, typeof(RateParameter))
        };

        public IReadOnlyList<DeviceCommandMetadata> DescribeCommands() => _commands;

        public bool IsStatusCommand(SimulatedCommandId commandId) => commandId == SimulatedCommandId.QueryStatus;

        public async Task<CommandResult> ExecuteAsync(DeviceCommand<SimulatedCommandId> command, IDeviceClient client, CancellationToken cancellationToken)
        {
            var payloadText = BuildPayload(command);
            var exchange = await client.ExchangeAsync(Encoding.UTF8.GetBytes(payloadText), cancellationToken).ConfigureAwait(false);
            if (!exchange.Success)
            {
                return CommandResult.Failed(exchange.Message ?? "Transport failed.");
            }

            return command.CommandId switch
            {
                SimulatedCommandId.QueryStatus => CommandResult.Completed("Status acquired.", BuildStatus()),
                SimulatedCommandId.Ping => CommandResult.Completed("Pong."),
                SimulatedCommandId.Start => CommandResult.Completed("Started."),
                SimulatedCommandId.Stop => CommandResult.Completed("Stopped."),
                SimulatedCommandId.SetRate => HandleSetRate(command),
                _ => CommandResult.Rejected("Unsupported command.")
            };
        }

        public DeviceStateSnapshot ApplyStatus(DeviceStateSnapshot state, DeviceCommand<SimulatedCommandId> command, CommandResult result)
        {
            if (result.Status != CommandStatus.Completed)
            {
                return state.With(health: HealthState.Degraded, detail: result.Message ?? "Status command failed.");
            }

            if (result.Data is SimulatedStatus status)
            {
                var nextHealth = status.Ready ? HealthState.Ready : HealthState.Degraded;
                var detail = status.Note ?? $"Temp={status.TemperatureC:F1}C";
                return state.With(connection: ConnectionState.Connected, health: nextHealth, detail: detail);
            }

            return state;
        }

        private static string BuildPayload(DeviceCommand<SimulatedCommandId> command)
        {
            if (command.Parameter is RateParameter rate)
            {
                return $"{command.CommandId}:{rate.RateHz}";
            }

            return command.CommandId.ToString();
        }

        private static SimulatedStatus BuildStatus()
        {
            var random = new Random();
            var ready = random.NextDouble() > 0.2;
            var temperature = 25 + random.NextDouble() * 10;
            return new SimulatedStatus(ready, temperature, ready ? "Ready" : "Warming up");
        }

        private static CommandResult HandleSetRate(DeviceCommand<SimulatedCommandId> command)
        {
            if (command.Parameter is not RateParameter rate)
            {
                return CommandResult.Failed("RateParameter required.");
            }

            if (rate.RateHz <= 0)
            {
                return CommandResult.Rejected("Rate must be > 0.");
            }

            return CommandResult.Completed($"Rate set to {rate.RateHz:F2} Hz");
        }
    }
}
