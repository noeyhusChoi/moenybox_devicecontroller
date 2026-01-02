using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Drivers.Printer
{
    internal static class PrinterCommandHandlers
    {
        public static IReadOnlyCollection<IDeviceCommandHandler> Create(PrinterClient client, string deviceKey)
        {
            var unknown = CommandResultFactory.UnknownCommand(deviceKey);
            return new IDeviceCommandHandler[]
            {
                new PrintContentHandler(client, unknown),
                new PrintTitleHandler(client, unknown),
                new CutHandler(client),
                new RestartHandler(),
                new QrHandler(client, unknown),
                new AlignHandler(client, unknown)
            };
        }

        private sealed class PrintContentHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            private readonly CommandResult _unknown;
            public PrintContentHandler(PrinterClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "PRINTCONTENT";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is string data
                    ? _client.PrintContentAsync(data, ct)
                    : Task.FromResult(_unknown);
        }

        private sealed class PrintTitleHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            private readonly CommandResult _unknown;
            public PrintTitleHandler(PrinterClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "PRINTTITLE";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is string data
                    ? _client.PrintTitleAsync(data, ct)
                    : Task.FromResult(_unknown);
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
            private readonly CommandResult _unknown;
            public QrHandler(PrinterClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "QR";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is string data
                    ? _client.PrintQrAutoSizeAsync(data, ct)
                    : Task.FromResult(_unknown);
        }

        private sealed class AlignHandler : IDeviceCommandHandler
        {
            private readonly PrinterClient _client;
            private readonly CommandResult _unknown;
            public AlignHandler(PrinterClient client, CommandResult unknown)
            {
                _client = client;
                _unknown = unknown;
            }
            public string Name => "ALIGN";
            public Task<CommandResult> HandleAsync(DeviceCommand command, CancellationToken ct)
                => command.Payload is int data
                    ? _client.AlignAsync(data, ct)
                    : Task.FromResult(_unknown);
        }
    }
}
