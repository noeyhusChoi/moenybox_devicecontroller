using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;

namespace KIOSK.Devices.Drivers.HCDM;

/// <summary>
/// HCDM-10K 프로토콜 클라이언트 (ACK/ENQ 핸드셰이크 + 프레임 송수신).
/// </summary>
internal sealed class Hcdm10kClient : IAsyncDisposable
{
    private readonly TransportChannel _channel;
    private bool _started;

    public event Action<string>? Log;

    public Hcdm10kClient(TransportChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started)
            return;

        _started = true;
        await _channel.StartAsync(ct).ConfigureAwait(false);
    }

    public async Task<CommandResult> SendCommandAsync(Hcdm10kCommand command, byte[]? data = null, int processTimeoutMs = 5000, CancellationToken ct = default)
    {
        if (!_started)
            await StartAsync(ct).ConfigureAwait(false);

        var frame = BuildFrame(command, data);
        Log?.Invoke($"[HCDM10K] TX {command}: {BitConverter.ToString(frame)}");
        await _channel.WriteAsync(frame, ct).ConfigureAwait(false);

        // ACK 대기 + ENQ 재시도
        if (!await WaitAckWithEnqAsync(ct).ConfigureAwait(false))
            throw new TimeoutException("ACK timeout.");

        int timeout = processTimeoutMs;
        int nak = 0;
        while (true)
        {
            byte[] full;
            full = await _channel.WaitAsync(
                f => f.Length > 0 && f.Span[0] == Hcdm10kProtocol.STX,
                timeoutMs: timeout,
                ct: ct).ConfigureAwait(false);

            Log?.Invoke($"[HCDM10K] RX: {BitConverter.ToString(full)}");

            var parsed = ParseResponse(full);
            if (parsed != null)
            {
                await _channel.WriteAsync(new byte[] { Hcdm10kProtocol.ACK }, ct).ConfigureAwait(false);  // 정상 응답에는 ACK
                return parsed.Value.ok
                    ? new CommandResult(true, Data: parsed.Value.data)
                    : new CommandResult(false, string.Empty, parsed.Value.data, new ErrorCode("DEV", "WITHDRAWAL", "COMMAND", "ERROR"));
            }

            nak++;
            if (nak > Hcdm10kProtocol.MaxNak)
                throw new InvalidOperationException("NAK limit exceeded.");

            await _channel.WriteAsync(new byte[] { Hcdm10kProtocol.NAK }, ct).ConfigureAwait(false);
            await Task.Delay(50, ct).ConfigureAwait(false);
        }
    }

    #region Protocol helpers

    public byte[] BuildFrame(Hcdm10kCommand command, byte[]? data = null)
    {
        data ??= Array.Empty<byte>();
        int len = 1 + data.Length; // CMD + DATA
        byte lenL = (byte)(len & 0xff);
        byte lenH = (byte)((len >> 8) & 0xff);

        // STX LENL LENH CMD DATA ETX CHK
        var buf = new byte[1 + 2 + len + 1 + 1];
        int idx = 0;
        buf[idx++] = Hcdm10kProtocol.STX;
        buf[idx++] = lenL;
        buf[idx++] = lenH;
        buf[idx++] = (byte)command;
        if (data.Length > 0) { Buffer.BlockCopy(data, 0, buf, idx, data.Length); idx += data.Length; }
        buf[idx++] = Hcdm10kProtocol.ETX;
        byte cs = 0;
        for (int i = 1; i < idx; i++) cs ^= buf[i];
        buf[idx++] = cs;
        return buf;
    }

    public (bool ok, string err, byte[] data)? ParseResponse(ReadOnlySpan<byte> frame)
    {
        // 최소: STX LENL LENH CMD ETX CHK
        if (frame.Length < 1 + 1 + 1 + 1 + 1 + 1) return null;
        if (frame[0] != Hcdm10kProtocol.STX) return null;

        int len = frame[1] | (frame[2] << 8);
        int expected = 1 + 2 + len + 1 + 1;
        if (frame.Length < expected) return null;

        // ETX 위치: 뒤에서 2바이트 앞(ETX, CHK)
        int etxPos = frame.Length - 2;
        if (frame[etxPos] != Hcdm10kProtocol.ETX) return null;

        // CHK 검사
        byte cs = 0;
        for (int i = 1; i <= etxPos; i++) cs ^= frame[i];
        if (cs != frame[etxPos + 1]) return null;

        char res = (char)frame[3];
        var data = frame.Slice(4, len - 1).ToArray();

        bool ok = res == 0x4F;
        return (ok, string.Empty, data);
    }

    private async Task<bool> WaitAckWithEnqAsync(CancellationToken ct)
    {
        // 우선 바로 확인
        if (await TryWaitByteAsync(Hcdm10kProtocol.ACK, Hcdm10kProtocol.AckWaitMs, ct).ConfigureAwait(false))
        {
            Log?.Invoke("[HCDM10K] ACK");
            return true;
        }

        for (int i = 0; i < Hcdm10kProtocol.MaxEnq; i++)
        {
            Log?.Invoke("[HCDM10K] ENQ");
            await _channel.WriteAsync(new byte[] { Hcdm10kProtocol.ENQ }, ct).ConfigureAwait(false);
            if (await TryWaitByteAsync(Hcdm10kProtocol.ACK, Hcdm10kProtocol.AckWaitMs, ct).ConfigureAwait(false))
                return true;
        }
        return false;
    }

    private async Task<bool> TryWaitByteAsync(byte expected, int timeoutMs, CancellationToken ct)
    {
        try
        {
            var frame = await _channel.WaitAsync(
                f => f.Length == 1 && f.Span[0] == expected,
                timeoutMs: timeoutMs,
                ct: ct).ConfigureAwait(false);
            return frame.Length == 1 && frame[0] == expected;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        try { await _channel.DisposeAsync().ConfigureAwait(false); } catch { }
    }
}
