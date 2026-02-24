using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.E200Z;

internal static class QrE200ZCommandHandlers
{
    public static IReadOnlyCollection<DeviceCommandDescriptor> SupportedCommands { get; } =
        new[]
        {
            new DeviceCommandDescriptor("RESTART", "재시작"),
            new DeviceCommandDescriptor("SCAN_ENABLE", "스캔 활성화"),
            new DeviceCommandDescriptor("SCAN_DISABLE", "스캔 비활성화"),
            new DeviceCommandDescriptor("START_DECODE", "디코드 시작"),
            new DeviceCommandDescriptor("STOP_DECODE", "디코드 중지"),
            new DeviceCommandDescriptor("RESET", "리셋"),
            //new DeviceCommandDescriptor("SET_HOST_TRIGGER", "Host Trigger 모드"),
            //new DeviceCommandDescriptor("SET_AUTO_TRIGGER", "Auto-Induction 모드"),
            //new DeviceCommandDescriptor("SET_PACKET_MODE", "Packet 모드"),
            //new DeviceCommandDescriptor("REQUEST_REVISION", "Revision 조회"),
        };

    public static IReadOnlyCollection<IDeviceCommandHandler> Create(E200ZClient client)
        => new IDeviceCommandHandler[]
        {
            new RestartHandler(),
            new ScanEnableHandler(client),
            new ScanDisableHandler(client),
            new StartDecodeHandler(client),
            new StopDecodeHandler(client),
            new ResetHandler(client),
            new SetHostTriggerHandler(client),
            new SetAutoTriggerHandler(client),
            new SetPacketModeHandler(client),
            new RequestRevisionHandler(client),
        };

    private sealed class RestartHandler : IDeviceCommandHandler
    {
        public string Name => "RESTART";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => Task.FromResult(new CommandResult(true));
    }

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
            => _client.SetHostTriggerModeAsync(ParseSaveToFlash(command.Payload), ct);
    }

    private sealed class SetAutoTriggerHandler : IDeviceCommandHandler
    {
        private readonly E200ZClient _client;
        public SetAutoTriggerHandler(E200ZClient client) => _client = client;
        public string Name => "SET_AUTO_TRIGGER";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => _client.SetAutoInductionTriggerModeAsync(ParseSaveToFlash(command.Payload), ct);
    }

    private sealed class SetPacketModeHandler : IDeviceCommandHandler
    {
        private readonly E200ZClient _client;
        public SetPacketModeHandler(E200ZClient client) => _client = client;
        public string Name => "SET_PACKET_MODE";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => _client.SetDecodeDataPacketFormatAsync(ParsePacketMode(command.Payload), ParseSaveToFlash(command.Payload), ct);
    }

    private sealed class RequestRevisionHandler : IDeviceCommandHandler
    {
        private readonly E200ZClient _client;
        public RequestRevisionHandler(E200ZClient client) => _client = client;
        public string Name => "REQUEST_REVISION";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => _client.RequestRevisionAsync(ct);
    }

    private static bool ParseSaveToFlash(object? payload)
    {
        if (payload is null)
            return true;

        if (payload is bool b)
            return b;

        if (payload is string s)
        {
            if (bool.TryParse(s, out var parsedBool))
                return parsedBool;
            if (s == "1")
                return true;
            if (s == "0")
                return false;
        }

        return true;
    }

    private static byte ParsePacketMode(object? payload)
    {
        if (payload is null)
            return 0x01;

        if (payload is byte b)
            return b;

        if (payload is int i && i >= byte.MinValue && i <= byte.MaxValue)
            return (byte)i;

        if (payload is string s)
        {
            var token = s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (byte.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                    return hex;
            }
            else if (byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec))
            {
                return dec;
            }
        }

        return 0x01;
    }
}
