using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.IdScanner;

namespace KIOSK.Device.Drivers.IdScanner
{
    internal static class IdScannerCommandHandlers
    {
        public static IReadOnlyCollection<DeviceCommandDescriptor> SupportedCommands { get; } =
            new[]
            {
                new DeviceCommandDescriptor("RESTART", "재시작"),
                new DeviceCommandDescriptor("SCANSTART", "스캔 시작"),
                new DeviceCommandDescriptor("SCANSTOP", "스캔 중지"),
                new DeviceCommandDescriptor("GETSCANSTATUS", "스캔 상태 조회"),
                new DeviceCommandDescriptor("SAVEIMAGE", "이미지 저장"),
            };

        public static IReadOnlyCollection<IDeviceCommandHandler> Create(IdScannerClient client)
            => new IDeviceCommandHandler[]
            {
                new RestartHandler(),
                new ScanStartHandler(client),
                new ScanStopHandler(client),
                new GetScanStatusHandler(client),
                new SaveImageHandler(client)
            };

        private sealed class RestartHandler : IDeviceCommandHandler
        {
            public string Name => "RESTART";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => Task.FromResult(new CommandResult(true));
        }

        private sealed class ScanStartHandler : IDeviceCommandHandler
        {
            private readonly IdScannerClient _client;
            public ScanStartHandler(IdScannerClient client) => _client = client;
            public string Name => "SCANSTART";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.StartScanAsync(ct);
        }

        private sealed class ScanStopHandler : IDeviceCommandHandler
        {
            private readonly IdScannerClient _client;
            public ScanStopHandler(IdScannerClient client) => _client = client;
            public string Name => "SCANSTOP";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.StopScanAsync(ct);
        }

        private sealed class GetScanStatusHandler : IDeviceCommandHandler
        {
            private readonly IdScannerClient _client;
            public GetScanStatusHandler(IdScannerClient client) => _client = client;
            public string Name => "GETSCANSTATUS";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.GetPresenceAsync(ct);
        }

        private sealed class SaveImageHandler : IDeviceCommandHandler
        {
            private readonly IdScannerClient _client;
            public SaveImageHandler(IdScannerClient client) => _client = client;
            public string Name => "SAVEIMAGE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.SaveImageAsync(ct);
        }
    }
}
