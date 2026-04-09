using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceKit.Transport;

namespace DeviceKit.Drivers.Printer;

/// <summary>
/// ESC/POS 기반 프린터 클라이언트.
/// - TransportChannel(Passthrough) 위에서 명령 송신/상태 요청을 수행한다.
/// </summary>
internal sealed class PrinterClient : IAsyncDisposable
{
    private readonly TransportChannel _channel;
    private readonly Encoding _ksEncoding = Encoding.GetEncoding("ks_c_5601-1987");
    private bool _started;

    public event Action<string>? Log;

    public PrinterClient(DeviceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _channel = new TransportChannel(StreamPortFactory.Create(descriptor));
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started)
            return;

        _started = true;
        await _channel.StartAsync(ct).ConfigureAwait(false);
    }

    public async Task<DeviceCommandResponse> RequestStatusAsync(CancellationToken ct = default)
    {
        byte[] cmd = { 0x1D, 0x72, 0x01 };

        await EnsureStartedAsync(ct).ConfigureAwait(false);

        var bytes = await _channel.SendAndWaitAsync(
            cmd,
            frame => frame.Length > 0,
            timeoutMs: PrinterDefaults.StatusTimeoutMs,
            ct: ct).ConfigureAwait(false);

        return bytes.Length > 0
            ? new DeviceCommandResponse(true, Data: bytes)
            : new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "STATUS", "TIMEOUT"));
    }

    public Task<DeviceCommandResponse> SetAlignAsync(PrinterAlignment align, CancellationToken ct = default)
    {
        return SendSimpleAsync(BuildAlignPacket(align), ct);
    }

    private static byte[] BuildAlignPacket(PrinterAlignment align)
    {
        byte value = align switch
        {
            PrinterAlignment.Center => 0x01,
            PrinterAlignment.Right => 0x02,
            _ => 0x00
        };

        return new byte[] { 0x1b, 0x61, value };
    }

    public Task<DeviceCommandResponse> CutAsync(CancellationToken ct = default)
    {
        byte[] buf = new byte[] { 0x1B, 0x69 };
        return SendSimpleAsync(buf, ct);
    }


    public async Task<DeviceCommandResponse> TextStyleAsync(int width, int height, int under, int bold, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        byte styleFlags1 = 0;
        if (bold == 1) styleFlags1 |= (1 << 3);      // Bit 3 for bold
        if (height == 1) styleFlags1 |= (1 << 4);    // Bit 4 for height
        if (width == 1) styleFlags1 |= (1 << 5);     // Bit 5 for width
        if (under == 1) styleFlags1 |= (1 << 7);     // Bit 7 for underline

        byte styleFlags2 = 0;
        if (width == 1) styleFlags2 |= (1 << 2);     // Bit 2 for width
        if (height == 1) styleFlags2 |= (1 << 3);    // Bit 3 for height
        if (under == 1) styleFlags2 |= (1 << 7);     // Bit 7 for underline

        await _channel.WriteAsync(new byte[] { 0x1b, 0x21, styleFlags1 }, ct).ConfigureAwait(false);
        await _channel.WriteAsync(new byte[] { 0x1c, 0x21, styleFlags2 }, ct).ConfigureAwait(false);

        return new DeviceCommandResponse(true);
    }

    public async Task<DeviceCommandResponse> PrintTextAsync(string data, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        byte[] buf = _ksEncoding.GetBytes(data);
        await _channel.WriteAsync(buf, ct).ConfigureAwait(false);
        Log?.Invoke($"[PRN] TX TEXT: {buf.Length} bytes");
        return new DeviceCommandResponse(true);
    }

    public async Task<DeviceCommandResponse> PrintContentAsync(string data, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);

        var res = await SetLeftMarginMmAsync(PrinterDefaults.LeftMarginMm, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        res = await SetAlignAsync(PrinterAlignment.Left, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        res = await TextStyleAsync(0, 0, 0, 0, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        return await PrintTextAsync(data, ct).ConfigureAwait(false);
    }

    public async Task<DeviceCommandResponse> PrintTitleAsync(string data, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);

        var res = await SetLeftMarginMmAsync(PrinterDefaults.LeftMarginMm, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        res = await SetAlignAsync(PrinterAlignment.Center, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        res = await TextStyleAsync(0, 1, 0, 1, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        return await PrintTextAsync(data, ct).ConfigureAwait(false);
    }

    public async Task<DeviceCommandResponse> PrintQrAutoSizeAsync(string data, CancellationToken ct = default)
    {
        if (data is null)
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "COMMAND", "ERROR"));

        int type = data.Length switch
        {
            <= 18 => 1,
            <= 54 => 3,
            <= 106 => 5,
            <= 230 => 9,
            _ => 9
        };

        await EnsureStartedAsync(ct).ConfigureAwait(false);
        var res = await SetLeftMarginMmAsync(PrinterDefaults.LeftMarginMm, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        res = await SetAlignAsync(PrinterAlignment.Center, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        return await PrintQrAsync(data, type, ct).ConfigureAwait(false);
    }

    public async Task<DeviceCommandResponse> AlignAsync(int align, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        var res = await SetLeftMarginMmAsync(PrinterDefaults.LeftMarginMm, ct).ConfigureAwait(false);
        if (!res.Success)
            return res;

        return await SetAlignAsync(ToAlignment(align), ct).ConfigureAwait(false);
    }

    public Task<DeviceCommandResponse> SetLeftMarginMmAsync(double mm, CancellationToken ct = default)
    {
        return SendSimpleAsync(BuildLeftMargin(mm), ct);
    }

    private static byte[] BuildLeftMargin(double mm)
    {
        if (mm < 0) mm = 0;
        int units = (int)Math.Round(mm / 0.125); // 1 unit = 0.125 mm
        if (units > 65535) units = 65535;

        byte nL = (byte)(units & 0xFF);
        byte nH = (byte)((units >> 8) & 0xFF);

        return new byte[] { 0x1D, 0x4C, nL, nH };
    }

    public async Task<DeviceCommandResponse> PrintQrAsync(string data, int version, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(data))
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "COMMAND", "ERROR"));

        byte[] buf = _ksEncoding.GetBytes(data);

        int maxLength = version switch
        {
            1 => 17,
            3 => 53,
            5 => 106,
            9 => 230,
            _ => -1
        };

        if (maxLength < 0)
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "COMMAND", "ERROR"));

        if (buf.Length > maxLength)
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "COMMAND", "ERROR"));

        byte mode = 0x02;
        byte dataLength = (byte)(buf.Length & 0xFF);
        byte type = (byte)version;
        byte[] cmd = new byte[] { 0x1A, 0x42, mode, dataLength, type };

        byte[] packet = new byte[cmd.Length + buf.Length];
        Buffer.BlockCopy(cmd, 0, packet, 0, cmd.Length);
        Buffer.BlockCopy(buf, 0, packet, cmd.Length, buf.Length);

        return await SendSimpleAsync(packet, ct).ConfigureAwait(false);
    }

    public async Task<DeviceCommandResponse> PrintBarcodeAsync(string data, int size, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(data))
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "PRINTER", "COMMAND", "ERROR"));

        byte[] buf = _ksEncoding.GetBytes(data);

        byte mode = 0x01;    // PDF417
        byte dataLength = (byte)(buf.Length & 0xFF);
        byte type = (byte)(size & 0xFF);
        byte[] cmd = new byte[] { 0x1A, 0x42, mode, dataLength, (byte)type, };

        byte[] packet = new byte[cmd.Length + buf.Length];
        Buffer.BlockCopy(cmd, 0, packet, 0, cmd.Length);
        Buffer.BlockCopy(buf, 0, packet, cmd.Length, buf.Length);

        return await SendSimpleAsync(packet, ct).ConfigureAwait(false);
    }

    private async Task<DeviceCommandResponse> SendSimpleAsync(byte[] payload, CancellationToken ct)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        Log?.Invoke($"[PRN] TX: {BitConverter.ToString(payload)}");
        await _channel.WriteAsync(payload, ct).ConfigureAwait(false);
        return new DeviceCommandResponse(true);
    }

    private Task EnsureStartedAsync(CancellationToken ct)
        => _started ? Task.CompletedTask : StartAsync(ct);

    private static PrinterAlignment ToAlignment(int align) =>
        align switch
        {
            1 => PrinterAlignment.Center,
            2 => PrinterAlignment.Right,
            _ => PrinterAlignment.Left
        };

    public ValueTask DisposeAsync()
    {
        return _channel.DisposeAsync();
    }

}
