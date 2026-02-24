using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.Printer
{
    internal static class PrinterCommandHandlers
    {
        public static IReadOnlyCollection<DeviceCommandDescriptor> SupportedCommands { get; } =
            new[]
            {
                new DeviceCommandDescriptor("RESTART", "재시작"),
                new DeviceCommandDescriptor("PRINTCONTENT", "본문 인쇄"),
                new DeviceCommandDescriptor("PRINTTITLE", "제목 인쇄"),
                new DeviceCommandDescriptor("CUT", "용지 컷"),
                new DeviceCommandDescriptor("QR", "QR 코드 인쇄"),
                new DeviceCommandDescriptor("ALIGN", "정렬 설정"),
            };

        public static IReadOnlyCollection<IDeviceCommandHandler> Create(PrinterClient client, string deviceKey)
        {
            var invalidPayload = CommandResults.InvalidPayload(deviceKey);
            return new IDeviceCommandHandler[]
            {
                new PrintContentHandler(client, invalidPayload),
                new PrintTitleHandler(client, invalidPayload),
                new CutHandler(client),
                new RestartHandler(),
                new QrHandler(client, invalidPayload),
                new AlignHandler(client, invalidPayload)
            };
        }

        private sealed class PrintContentHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            private readonly CommandResult _invalidPayload;
            public PrintContentHandler(PrinterClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "PRINTCONTENT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is string data
                    ? _client.PrintContentAsync(data, ct)
                    : Task.FromResult(_invalidPayload);
        }

        private sealed class PrintTitleHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            private readonly CommandResult _invalidPayload;
            public PrintTitleHandler(PrinterClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "PRINTTITLE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is string data
                    ? _client.PrintTitleAsync(data, ct)
                    : Task.FromResult(_invalidPayload);
        }

        private sealed class CutHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            public CutHandler(PrinterClient client) => _client = client;
            public string Name => "CUT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => _client.CutAsync(ct);
        }

        private sealed class RestartHandler : IDeviceCommandHandler
        {
            public string Name => "RESTART";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => Task.FromResult(new CommandResult(true));
        }

        private sealed class QrHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            private readonly CommandResult _invalidPayload;
            public QrHandler(PrinterClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "QR";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is string data
                    ? _client.PrintQrAutoSizeAsync(data, ct)
                    : Task.FromResult(_invalidPayload);
        }

        private sealed class AlignHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            private readonly CommandResult _invalidPayload;
            public AlignHandler(PrinterClient client, CommandResult invalidPayload)
            {
                _client = client;
                _invalidPayload = invalidPayload;
            }
            public string Name => "ALIGN";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is int data
                    ? _client.AlignAsync(data, ct)
                    : Task.FromResult(_invalidPayload);
        }
    }
}
