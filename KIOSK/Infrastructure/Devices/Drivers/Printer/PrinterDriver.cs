using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;
using KIOSK.Device.Drivers.Printer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 프린터 드라이버: 장치 정책/상태/명령 라우팅 담당.
/// 실제 ESC/POS 송수신은 PrinterClient(TransportChannel 기반)에 위임한다.
/// </summary>
public sealed class PrinterDriver : DeviceBase
{
    private PrinterClient? _client;
    private CommandDispatcher? _dispatcher;
    private readonly ILogger<PrinterDriver> _logger;

    public event Action<string>? Log;

    public PrinterDriver(DeviceDescriptor desc, ITransport transport, ILogger<PrinterDriver>? logger = null)
        : base(desc, transport)
    {
        _logger = logger ?? NullLogger<PrinterDriver>.Instance;
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var channel = CreateChannel(); // Passthrough framer
            var client = new PrinterClient(channel);
            client.Log += OnClientLog;
            _client = client;
            _dispatcher = new CommandDispatcher(
                PrinterCommandHandlers.Create(client, Descriptor.DeviceKey),
                CreateUnknownCommandResult);
            await client.StartAsync(ct).ConfigureAwait(false);

            return CreateSnapshot();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            _logger.LogError(ex, "Printer initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("Printer not initialized.");

            var res = await client.RequestStatusAsync(ct).ConfigureAwait(false);
            if (res.Success && res.Data is byte[] bytes && bytes.Length > 0)
            {
                ParseStatus(bytes[0], alerts);
            }
            else
            {
                alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer status failed. device={Device} model={Model}", Name, Model);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;

            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED"));

            if (_dispatcher is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED"));

            return await _dispatcher.DispatchAsync(command, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogWarning("Printer command timeout. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT"), Retryable: true);
        }
        catch (Exception ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogError(ex, "Printer command failed. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "ERROR"));
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeClientAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private async Task DisposeClientAsync()
    {
        if (_client is null)
            return;

        try
        {
            _client.Log -= OnClientLog;
            await _client.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }
        _client = null;
        _dispatcher = null;
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);

    private void ParseStatus(byte statusByte, List<StatusEvent> alerts)
    {
        var flags = (PrinterStatusFlags)statusByte;

        if (flags.HasFlag(PrinterStatusFlags.PaperOut))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "NO_PAPER"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.HeadUp))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "HEAD_UP"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.PaperError))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "PAPER_ERROR"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.PaperNearEnd))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "PAPER_NEAR_END"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.Printing))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "PRINTING"), string.Empty, Severity.Info));
        if (flags.HasFlag(PrinterStatusFlags.CutterError))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "CUTTER"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.AuxPaperPresent))
            alerts.Add(CreateAlert(new ErrorCode("DEV", "PRINTER", "STATUS", "AUX_PAPER_PRESENT"), string.Empty, Severity.Warning));
    }

}
