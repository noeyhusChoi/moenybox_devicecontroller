using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;

namespace KIOSK.Device.Drivers.E200Z;

/// <summary>
/// E200Z 프로토콜 통신 엔진(요청-응답 + 비동기 수신/파싱).
/// </summary>
internal sealed class E200ZClient : IAsyncDisposable
{
    private readonly TransportChannel _channel;
    private readonly Encoding _decodeEncoding = Encoding.GetEncoding("euc-kr");
    private bool _started;

    public event Action<string>? Log;
    public event EventHandler<DecodeMessage>? Decoded;
    public event Action<string>? RevisionReceived;

    public E200ZClient(TransportChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started)
            return;

        _started = true;
        _channel.FrameReceived += OnFrameReceived;
        await _channel.StartAsync(ct).ConfigureAwait(false);
    }

    public Task<CommandResult> ScanEnableAsync(CancellationToken ct) =>
        SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.SCAN_ENABLE), SsiOpcode.CMD_ACK, E200ZTimeouts.DefaultCommandMs, ct);

    public Task<CommandResult> ScanDisableAsync(CancellationToken ct) =>
        SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.SCAN_DISABLE), SsiOpcode.CMD_ACK, E200ZTimeouts.DefaultCommandMs, ct);

    public Task<CommandResult> StartDecodeAsync(CancellationToken ct) =>
        SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.START_DECODE), SsiOpcode.CMD_ACK, E200ZTimeouts.DefaultCommandMs, ct);

    public Task<CommandResult> StopDecodeAsync(CancellationToken ct) =>
        SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.STOP_DECODE), SsiOpcode.CMD_ACK, E200ZTimeouts.DefaultCommandMs, ct);

    public Task<CommandResult> ResetAsync(CancellationToken ct) =>
        SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.RESET), SsiOpcode.CMD_ACK, E200ZTimeouts.ResetMs, ct);

    public Task<CommandResult> RequestRevisionAsync(CancellationToken ct) =>
        SendPacketAsync(SsiPacket.CreateSimple(SsiOpcode.REQUEST_REVISION), SsiOpcode.REPLY_REVISION, E200ZTimeouts.DefaultCommandMs, ct);

    public Task<CommandResult> SetHostTriggerModeAsync(bool saveToFlash, CancellationToken ct)
    {
        var pkt = SsiPacket.CreateParamByte(0x8A, 0x08, saveToFlash);
        return SendPacketAsync(pkt, SsiOpcode.CMD_ACK, E200ZTimeouts.DefaultCommandMs, ct);
    }

    public Task<CommandResult> SetAutoInductionTriggerModeAsync(bool saveToFlash, CancellationToken ct)
    {
        var pkt = SsiPacket.CreateParamByte(0x8A, 0x09, saveToFlash);
        return SendPacketAsync(pkt, SsiOpcode.CMD_ACK, E200ZTimeouts.DefaultCommandMs, ct);
    }

    public Task<CommandResult> SetDecodeDataPacketFormatAsync(byte value, bool saveToFlash, CancellationToken ct)
    {
        var pkt = SsiPacket.CreateParamByte(0xEE, value, saveToFlash);
        return SendPacketAsync(pkt, SsiOpcode.CMD_ACK, E200ZTimeouts.DefaultCommandMs, ct);
    }

    private async Task<CommandResult> SendPacketAsync(SsiPacket packet, SsiOpcode expected, int timeoutMs, CancellationToken ct)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        var tx = packet.ToBytes();
        Log?.Invoke($"[E200Z] TX: {BitConverter.ToString(tx)}");

        var bytes = await _channel.SendAndWaitAsync(
            tx,
            frame => TryParseFrame(frame.Span, out var parsed) && parsed.Opcode == expected,
            timeoutMs,
            ct).ConfigureAwait(false);

        if (!TryParseFrame(bytes, out var parsedResp))
            throw new InvalidOperationException("Invalid response frame.");

        Log?.Invoke($"[E200Z] RX: Op=0x{(byte)parsedResp.Opcode:X2}, Len={parsedResp.Data.Length}");

        HandlePacket(parsedResp);
        return new CommandResult(true, Data: parsedResp.Data);
    }

    private void OnFrameReceived(ReadOnlyMemory<byte> frame)
    {
        try
        {
            if (!TryParseFrame(frame.Span, out var parsed))
                return;

            HandlePacket(parsed);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[E200Z] RX handler error: {ex.Message}");
        }
    }

    private void HandlePacket(SsiParsed parsed)
    {
        switch (parsed.Opcode)
        {
            case SsiOpcode.DECODE_DATA:
            case SsiOpcode.DECODE_DATA_TWO:
                HandleDecode(parsed);
                _ = SendAckAsync(); // fire-and-forget
                break;

            case SsiOpcode.REPLY_REVISION:
                HandleRevisionReply(parsed.Data);
                break;

            case SsiOpcode.CMD_ACK:
                Log?.Invoke("[E200Z] Got ACK from engine");
                break;

            case SsiOpcode.CMD_NAK:
                HandleNak(parsed.Data);
                break;

            default:
                Log?.Invoke($"[E200Z] Packet: Opcode=0x{(byte)parsed.Opcode:X2}, Ext={parsed.Extended}, DataLen={parsed.Data.Length}");
                break;
        }
    }

    private void HandleDecode(SsiParsed parsed)
    {
        try
        {
            if (parsed.Data.Length < 1)
            {
                Log?.Invoke("[E200Z] Decode packet with no data");
                return;
            }

            byte barcodeType = parsed.Data[0];
            byte[] textBytes = new byte[parsed.Data.Length - 1];
            if (textBytes.Length > 0)
                Array.Copy(parsed.Data, 1, textBytes, 0, textBytes.Length);

            string text = _decodeEncoding.GetString(textBytes);

            var msg = new DecodeMessage
            {
                IsExtended = parsed.Extended,
                BarcodeType = barcodeType,
                Text = text,
                RawData = textBytes
            };

            Log?.Invoke($"[E200Z] DECODE: Type=0x{barcodeType:X2}, Text=\"{text}\"");
            Decoded?.Invoke(this, msg);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[E200Z] Decode handler error: {ex.Message}");
        }
    }

    private void HandleRevisionReply(byte[] data)
    {
        string rev = Encoding.ASCII.GetString(data);
        RevisionReceived?.Invoke(rev);
        Log?.Invoke($"[E200Z] Revision: {rev}");
    }

    private void HandleNak(byte[] data)
    {
        byte cause = data.Length > 0 ? data[0] : (byte)0xFF;
        Log?.Invoke($"[E200Z] NAK received. Cause=0x{cause:X2}");
    }

    private async Task SendAckAsync()
    {
        try
        {
            await EnsureStartedAsync(CancellationToken.None).ConfigureAwait(false);
            var pkt = new SsiPacket
            {
                Length = 0x04,
                Opcode = (byte)SsiOpcode.CMD_ACK,
                Source = 0x04, // Host
                Status = 0x00,
                Data = Array.Empty<byte>()
            };

            await _channel.WriteAsync(pkt.ToBytes(), CancellationToken.None).ConfigureAwait(false);
            Log?.Invoke("[E200Z] TX: CMD_ACK sent");
        }
        catch (OperationCanceledException)
        {
            // 재연결/종료 중에는 정상적으로 발생할 수 있음(노이즈 로그 방지)
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[E200Z] SendAck error: {ex.Message}");
        }
    }

    private Task EnsureStartedAsync(CancellationToken ct)
        => _started ? Task.CompletedTask : StartAsync(ct);

    private static bool TryParseFrame(ReadOnlySpan<byte> frame, out SsiParsed parsed)
    {
        parsed = default;
        if (frame.Length < 6)
            return false;

        if (frame[0] == 0xFF)
        {
            if (frame.Length < 7) return false;
            ushort length2 = (ushort)((frame[2] << 8) | frame[3]);
            int totalLen = length2 + 2;
            if (frame.Length < totalLen) return false;

            byte opcode = frame[4];
            byte source = frame[5];
            byte status = frame[6];
            int dataLen = length2 - 7;
            if (dataLen < 0) dataLen = 0;
            var data = frame.Slice(7, Math.Max(0, dataLen)).ToArray();

            parsed = new SsiParsed((SsiOpcode)opcode, source, status, data, true);
            return true;
        }

        byte length = frame[0];
        int total = length + 2;
        if (frame.Length < total) return false;
        if (length < 4) return false;

        byte op = frame[1];
        byte src = frame[2];
        byte st = frame[3];
        int dl = length - 4;
        var payload = dl > 0 ? frame.Slice(4, dl).ToArray() : Array.Empty<byte>();
        parsed = new SsiParsed((SsiOpcode)op, src, st, payload, false);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_started)
            _channel.FrameReceived -= OnFrameReceived;

        await _channel.DisposeAsync().ConfigureAwait(false);
    }
}
