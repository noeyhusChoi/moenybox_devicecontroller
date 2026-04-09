using DeviceKit.Drivers.Printer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceKit.Drivers;

/// <summary>
/// 프린터 드라이버: 장치 정책/상태/명령 라우팅 담당.
/// 실제 ESC/POS 송수신은 PrinterClient(TransportChannel 기반)에 위임한다.
/// </summary>
internal sealed class PrinterDriver : DeviceDriverBase, IPrinterDriver
{
    public static IReadOnlyDictionary<string, DeviceCommandSpec> CommandTable { get; } =
        new Dictionary<string, DeviceCommandSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["RESTART"] = DeviceCommandSpec.Create<IDeviceDriver>(
                "RESTART",
                "재시작",
                static (_, _, _) => Task.FromResult(new DeviceCommandResponse(true))),
            [Printer.PrinterCommands.PrintContentName] = DeviceCommandSpec.Create<IPrinterDriver>(
                Printer.PrinterCommands.PrintContentName,
                "본문 인쇄",
                static (driver, command, ct) => driver.PrintContentAsync((string)command.Payload!, ct),
                payloadValidator: static payload => payload is string),
            [Printer.PrinterCommands.PrintTitleName] = DeviceCommandSpec.Create<IPrinterDriver>(
                Printer.PrinterCommands.PrintTitleName,
                "제목 인쇄",
                static (driver, command, ct) => driver.PrintTitleAsync((string)command.Payload!, ct),
                payloadValidator: static payload => payload is string),
            [Printer.PrinterCommands.CutName] = DeviceCommandSpec.Create<IPrinterDriver>(
                Printer.PrinterCommands.CutName,
                "용지 컷",
                static (driver, _, ct) => driver.CutAsync(ct)),
            ["QR"] = DeviceCommandSpec.Create<PrinterDriver>(
                "QR",
                "QR 코드 인쇄",
                static (driver, command, ct) => driver.GetRequiredClient().PrintQrAutoSizeAsync((string)command.Payload!, ct),
                payloadValidator: static payload => payload is string),
            ["ALIGN"] = DeviceCommandSpec.Create<PrinterDriver>(
                "ALIGN",
                "정렬 설정",
                static (driver, command, ct) => driver.GetRequiredClient().AlignAsync((int)command.Payload!, ct),
                payloadValidator: static payload => payload is int),
        };

    private PrinterClient? _client;
    protected override string ErrorTarget => "PRINTER";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    public event Action<string>? Log;

    public PrinterDriver(DeviceDescriptor desc, ILogger<PrinterDriver>? logger = null)
        : base(desc, logger ?? NullLogger<PrinterDriver>.Instance)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new PrinterClient(Descriptor);
            client.Log += OnClientLog;
            _client = client;
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
            Logger.LogError(ex, "Printer initialize failed. device={Device} model={Model}", Name, Model);
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
                alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "ERROR"), res.Message ?? "Printer status request failed.", Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning(ex, "Printer status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "TIMEOUT"), ex.Message, Severity.Warning));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Printer status failed. device={Device} model={Model}", Name, Model);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public Task<DeviceCommandResponse> PrintTitleAsync(string content, CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.PrintTitleAsync(content, ct);
    }

    public Task<DeviceCommandResponse> PrintContentAsync(string content, CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.PrintContentAsync(content, ct);
    }

    public Task<DeviceCommandResponse> CutAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.CutAsync(ct);
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
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);

    private void ParseStatus(byte statusByte, List<StatusEvent> alerts)
    {
        var flags = (PrinterStatusFlags)statusByte;

        if (flags.HasFlag(PrinterStatusFlags.PaperOut))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "NO_PAPER"), "Printer is out of paper.", Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.HeadUp))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "HEAD_UP"), "Printer head is open.", Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.PaperError))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "PAPER_ERROR"), "Printer reported a paper error.", Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.PaperNearEnd))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "PAPER_NEAR_END"), "Printer paper is near end.", Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.Printing))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "PRINTING"), "Printer is currently printing.", Severity.Info));
        if (flags.HasFlag(PrinterStatusFlags.CutterError))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "CUTTER"), "Printer cutter error detected.", Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.AuxPaperPresent))
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "AUX_PAPER_PRESENT"), "Auxiliary paper is present.", Severity.Warning));
    }

    private PrinterClient GetRequiredClient()
        => _client ?? throw new InvalidOperationException("Printer not initialized.");

}
