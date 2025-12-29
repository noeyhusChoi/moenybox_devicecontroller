using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;
using KIOSK.Device.Drivers.Printer;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 프린터 드라이버: 장치 정책/상태/명령 라우팅 담당.
/// 실제 ESC/POS 송수신은 PrinterClient(TransportChannel 기반)에 위임한다.
/// </summary>
public sealed class PrinterDriver : DeviceBase
{
    private PrinterClient? _client;

    public event Action<string>? Log;

    public PrinterDriver(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
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
            await client.StartAsync(ct).ConfigureAwait(false);

            return CreateSnapshot();
        }
        catch (Exception)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            return CreateSnapshot(new[]
            {
                CreateAlarm(new ErrorCode("DEV", "PRINTER", "CONNECT", "FAIL"), string.Empty)
            });
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alarms = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("Printer not initialized.");

            var res = await client.RequestStatusAsync(ct).ConfigureAwait(false);
            if (res.Success && res.Data is byte[] bytes && bytes.Length > 0)
            {
                ParseStatus(bytes[0], alarms);
            }
            else
            {
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "TIMEOUT", "RESPONSE"), string.Empty, Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            throw;
        }

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "CONNECT", "FAIL"));

            var client = _client;

            switch (command)
            {
                case { Name: string name, Payload: string data } when name.Equals("PRINTCONTENT", StringComparison.OrdinalIgnoreCase):
                    return await client.PrintContentAsync(data, ct).ConfigureAwait(false);

                case { Name: string name, Payload: string data } when name.Equals("PRINTTITLE", StringComparison.OrdinalIgnoreCase):
                    return await client.PrintTitleAsync(data, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("CUT", StringComparison.OrdinalIgnoreCase):
                    return await client.CutAsync(ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("RESTART", StringComparison.OrdinalIgnoreCase):
                    return new CommandResult(true);

                case { Name: string name, Payload: string data } when name.Equals("QR", StringComparison.OrdinalIgnoreCase):
                    return await client.PrintQrAutoSizeAsync(data, ct).ConfigureAwait(false);

                case { Name: string name, Payload: int data } when name.Equals("ALIGN", StringComparison.OrdinalIgnoreCase):
                    return await client.AlignAsync(data, ct).ConfigureAwait(false);

                default:
                    return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "ERROR", "UNKNOWN_COMMAND"));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "STATUS", "ERROR"));
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
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);

    private void ParseStatus(byte statusByte, List<StatusEvent> alarms)
    {
        var flags = (PrinterStatusFlags)statusByte;

        if (flags.HasFlag(PrinterStatusFlags.PaperOut))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "STATUS", "NO_PAPER"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.HeadUp))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "STATUS", "HEAD_UP"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.PaperError))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "STATUS", "PAPER_ERROR"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.PaperNearEnd))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "STATUS", "PAPER_NEAR_END"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.Printing))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "STATUS", "PRINTING"), string.Empty, Severity.Info));
        if (flags.HasFlag(PrinterStatusFlags.CutterError))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "STATUS", "CUTTER"), string.Empty, Severity.Warning));
        if (flags.HasFlag(PrinterStatusFlags.AuxPaperPresent))
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "PRINTER", "STATUS", "AUX_PAPER_PRESENT"), string.Empty, Severity.Warning));
    }

}
