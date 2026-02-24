using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.EM20;

internal static class QrEm20CommandHandlers
{
    public static IReadOnlyCollection<DeviceCommandDescriptor> SupportedCommands { get; } =
        new[]
        {
            new DeviceCommandDescriptor("RESTART", "재시작"),
            new DeviceCommandDescriptor("SCAN_ENABLE", "스캔 활성화"),
            new DeviceCommandDescriptor("SCAN_DISABLE", "스캔 비활성화"),
            new DeviceCommandDescriptor("SCAN_ONCE", "단일 스캔"),
        };

    public static IReadOnlyCollection<IDeviceCommandHandler> Create(Em20Client client)
        => new IDeviceCommandHandler[]
        {
            new RestartHandler(),
            new ScanEnableHandler(client),
            new ScanDisableHandler(client),
            new ScanOnceHandler(client)
        };

    private sealed class RestartHandler : IDeviceCommandHandler
    {
        public string Name => "RESTART";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => Task.FromResult(new CommandResult(true));
    }

    private sealed class ScanEnableHandler : IDeviceCommandHandler
    {
        private readonly Em20Client _client;
        public ScanEnableHandler(Em20Client client) => _client = client;
        public string Name => "SCAN_ENABLE";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => _client.TriggerAsync(true, ct);
    }

    private sealed class ScanDisableHandler : IDeviceCommandHandler
    {
        private readonly Em20Client _client;
        public ScanDisableHandler(Em20Client client) => _client = client;
        public string Name => "SCAN_DISABLE";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => _client.TriggerAsync(false, ct);
    }

    private sealed class ScanOnceHandler : IDeviceCommandHandler
    {
        private readonly Em20Client _client;
        public ScanOnceHandler(Em20Client client) => _client = client;
        public string Name => "SCAN_ONCE";
        public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
            => _client.ScanOnceAsync(ct);
    }
}
