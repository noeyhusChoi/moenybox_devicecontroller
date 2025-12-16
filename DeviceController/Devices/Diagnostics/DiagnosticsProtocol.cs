using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.Devices.Diagnostics
{
    public class DiagnosticsProtocol : IDeviceProtocol<DiagnosticsCommandId>
    {
        private readonly IReadOnlyList<DeviceCommandMetadata> _commands = new[]
        {
            new DeviceCommandMetadata(DiagnosticsCommandId.QueryStatus, "Diag Status", "Poll diagnostics status.", true),
            new DeviceCommandMetadata(DiagnosticsCommandId.Reset, "Reset", "Reset device.", false),
            new DeviceCommandMetadata(DiagnosticsCommandId.CalibrateOffset, "Calibrate", "Calibrate voltage offset (mV).", false, typeof(CalibrateParameter)),
            new DeviceCommandMetadata(DiagnosticsCommandId.GetVersion, "Get Version", "Read firmware version.", false)
        };

        public IReadOnlyList<DeviceCommandMetadata> DescribeCommands() => _commands;

        public bool IsStatusCommand(DiagnosticsCommandId commandId) => commandId == DiagnosticsCommandId.QueryStatus;

        public async Task<CommandResult> ExecuteAsync(DeviceCommand<DiagnosticsCommandId> command, IDeviceClient client, CancellationToken cancellationToken)
        {
            var payload = BuildPayload(command);
            var exchange = await client.ExchangeAsync(Encoding.UTF8.GetBytes(payload), cancellationToken).ConfigureAwait(false);
            if (!exchange.Success)
            {
                return CommandResult.Failed(exchange.Message ?? "Diagnostics transport failed.");
            }

            return command.CommandId switch
            {
                DiagnosticsCommandId.QueryStatus => CommandResult.Completed("Diagnostics status", BuildStatus()),
                DiagnosticsCommandId.Reset => CommandResult.Completed("Reset issued."),
                DiagnosticsCommandId.CalibrateOffset => HandleCalibrate(command),
                DiagnosticsCommandId.GetVersion => CommandResult.Completed("Version retrieved.", "v1.2.3"),
                _ => CommandResult.Rejected("Unsupported diagnostics command.")
            };
        }

        public DeviceStateSnapshot ApplyStatus(DeviceStateSnapshot state, DeviceCommand<DiagnosticsCommandId> command, CommandResult result)
        {
            if (result.Status != CommandStatus.Completed || result.Data is not DiagnosticsStatus status)
            {
                return state.With(health: HealthState.Degraded, detail: result.Message ?? "Diagnostics status failed.");
            }

            var health = status.Ready ? HealthState.Ready : HealthState.Degraded;
            var detail = $"V={status.Voltage:F2}V FW={status.Firmware}";
            return state.With(connection: ConnectionState.Connected, health: health, detail: detail);
        }

        private static string BuildPayload(DeviceCommand<DiagnosticsCommandId> command)
        {
            if (command.Parameter is CalibrateParameter calibrate)
            {
                return $"{command.CommandId}:{calibrate.OffsetMv:F1}";
            }

            return command.CommandId.ToString();
        }

        private static DiagnosticsStatus BuildStatus()
        {
            var random = new Random();
            var ready = random.NextDouble() > 0.15;
            var voltage = 4.8 + random.NextDouble() * 0.6;
            return new DiagnosticsStatus(ready, voltage, ready ? "1.2.3" : "1.2.3-beta");
        }

        private static CommandResult HandleCalibrate(DeviceCommand<DiagnosticsCommandId> command)
        {
            if (command.Parameter is not CalibrateParameter parameter)
            {
                return CommandResult.Rejected("CalibrateParameter required.");
            }

            if (Math.Abs(parameter.OffsetMv) > 500)
            {
                return CommandResult.Rejected("Offset range exceeded (+/-500mV).");
            }

            return CommandResult.Completed($"Offset set to {parameter.OffsetMv:F1} mV");
        }
    }
}
