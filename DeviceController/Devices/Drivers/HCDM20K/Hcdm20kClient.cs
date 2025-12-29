using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;

namespace KIOSK.Devices.Drivers.HCDM20K;

internal sealed class Hcdm20kClient : IAsyncDisposable
{
    private const byte STX = 0x02;
    private const byte ENQ = 0x05;
    private const byte ACK = 0x06;
    private const byte NAK = 0x15;

    private const int AckWaitMs = 300;
    private const int MaxNak = 1;

    private readonly TransportChannel _channel;
    private bool _started;

    public Hcdm20kClient(TransportChannel channel)
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

    public async Task<CommandResult> SendCommandAsync(
        Hcdm20kCommand command,
        byte[]? payload,
        int processTimeoutMs,
        CancellationToken ct,
        bool isLongOpWithEnq = false)
    {
        try
        {
            if (!_started)
                await StartAsync(ct).ConfigureAwait(false);

            var frame = Hcdm20kFramer.BuildFrame(command, payload ?? Array.Empty<byte>());
            Trace.WriteLine($"[HCDM20K] TX: {BitConverter.ToString(frame)}");
            await _channel.WriteAsync(frame, ct).ConfigureAwait(false);

            for (int attempt = 0; attempt <= MaxNak; attempt++)
            {
                var ack = await TryWaitByteAsync(AckWaitMs, ct).ConfigureAwait(false);
                if (ack == ACK) break;
                if (ack == NAK || ack < 0)
                {
                    Trace.WriteLine("[HCDM20K] ACK timeout/NAK, retry send");
                    await _channel.WriteAsync(frame, ct).ConfigureAwait(false);
                    continue;
                }
                attempt--;
            }

            var deadline = Stopwatch.StartNew();
            int extendMs = processTimeoutMs;

            while (true)
            {
                int remaining = Math.Max(1, extendMs - (int)deadline.ElapsedMilliseconds);
                byte[] frameBytes;
                try
                {
                    frameBytes = await _channel.WaitAsync(
                        f => (f.Length == 1 && f.Span[0] == ENQ) || (f.Length > 0 && f.Span[0] == STX),
                        timeoutMs: remaining,
                        ct: ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested)
                        throw;

                    return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "TIMEOUT", "RESPONSE"), Retryable: true);
                }
                catch (TimeoutException)
                {
                    return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "TIMEOUT", "RESPONSE"), Retryable: true);
                }

                if (frameBytes.Length == 1 && frameBytes[0] == ENQ)
                {
                    extendMs += 3000;
                    Trace.WriteLine("[HCDM20K] ENQ received (+3s)");
                    continue;
                }

                if (frameBytes.Length > 0 && frameBytes[0] == STX)
                {
                    Trace.WriteLine($"[HCDM20K] RX: {BitConverter.ToString(frameBytes)}");

                    var parsed = Hcdm20kFramer.ParseResponse(frameBytes);
                    if (parsed == null)
                        return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"));

                    await _channel.WriteAsync(new byte[] { ACK }, ct).ConfigureAwait(false);

                    return parsed.Value.ok
                        ? new CommandResult(true, Data: parsed.Value.data)
                        : new CommandResult(false, string.Empty, parsed.Value.data, new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"));
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "HCDM", "STATUS", "ERROR"));
        }
    }

    private async Task<int> TryWaitByteAsync(int timeoutMs, CancellationToken ct)
    {
        try
        {
            var frame = await _channel.WaitAsync(
                f => f.Length == 1,
                timeoutMs: timeoutMs,
                ct: ct).ConfigureAwait(false);
            return frame.Length == 1 ? frame[0] : -1;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return -1;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { await _channel.DisposeAsync().ConfigureAwait(false); } catch { }
    }
}
