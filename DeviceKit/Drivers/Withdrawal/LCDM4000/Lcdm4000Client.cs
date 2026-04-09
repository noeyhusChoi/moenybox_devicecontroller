using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceKit.Drivers.LCDM4000;

internal sealed class Lcdm4000Client : IAsyncDisposable
{
    private readonly TransportChannel _channel;
    private readonly ILogger _logger;
    private bool _started;

    public Lcdm4000Client(DeviceDescriptor descriptor, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _logger = logger ?? NullLogger.Instance;
        _channel = new TransportChannel(StreamPortFactory.Create(descriptor), new Lcdm4000Framer());
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started)
            return;

        _started = true;
        await _channel.StartAsync(ct).ConfigureAwait(false);
    }

    public async Task<Lcdm4000Response> SendCommandAsync(
        Lcdm4000Command command,
        byte[]? payload = null,
        int processTimeoutMs = 2000,
        CancellationToken ct = default,
        bool expectResponse = true)
    {
        if (!_started)
            await StartAsync(ct).ConfigureAwait(false);

        var commandPayload = payload ?? Array.Empty<byte>();
        var frame = BuildFrame(command, commandPayload);

        for (int attempt = 1; attempt <= Lcdm4000Protocol.MaxRetry; attempt++)
        {
            _logger.LogDebug("[LCDM4000] TX attempt={Attempt} cmd=0x{Command:X2} frame={Frame}", attempt, (byte)command, BitConverter.ToString(frame));
            await _channel.WriteAsync(frame, ct).ConfigureAwait(false);

            var control = await TryWaitControlByteAsync(Lcdm4000Protocol.AckWaitMs, ct).ConfigureAwait(false);
            if (control == Lcdm4000Protocol.ACK)
            {
                if (!expectResponse)
                    return new Lcdm4000Response((byte)command, Lcdm4000Protocol.NoError, Array.Empty<byte>());

                return await ReadResponseAsync(command, processTimeoutMs, ct).ConfigureAwait(false);
            }

            _logger.LogDebug("[LCDM4000] ACK wait failed. cmd=0x{Command:X2} control=0x{Control:X2}", (byte)command, control);
        }

        throw new TimeoutException("LCDM4000 ACK timeout.");
    }

    public byte[] BuildFrame(Lcdm4000Command command, byte[] payload)
    {
        payload ??= Array.Empty<byte>();

        var buffer = new byte[5 + payload.Length + 1];
        int index = 0;
        buffer[index++] = Lcdm4000Protocol.EOT;
        buffer[index++] = Lcdm4000Protocol.DeviceId;
        buffer[index++] = Lcdm4000Protocol.STX;
        buffer[index++] = (byte)command;

        if (payload.Length > 0)
        {
            Buffer.BlockCopy(payload, 0, buffer, index, payload.Length);
            index += payload.Length;
        }

        buffer[index++] = Lcdm4000Protocol.ETX;
        buffer[index] = ComputeBcc(buffer.AsSpan(0, index));
        return buffer;
    }

    private async Task<Lcdm4000Response> ReadResponseAsync(Lcdm4000Command expectedCommand, int processTimeoutMs, CancellationToken ct)
    {
        for (int attempt = 1; attempt <= Lcdm4000Protocol.MaxRetry; attempt++)
        {
            var frame = await _channel.WaitAsync(
                f => f.Length == 1 || (f.Length > 0 && f.Span[0] == Lcdm4000Protocol.SOH),
                timeoutMs: processTimeoutMs,
                ct: ct).ConfigureAwait(false);

            if (frame.Length == 1)
            {
                _logger.LogDebug("[LCDM4000] Ignoring control byte during response wait. byte=0x{Byte:X2}", frame[0]);
                attempt--;
                continue;
            }

            _logger.LogDebug("[LCDM4000] RX frame={Frame}", BitConverter.ToString(frame.ToArray()));

            if (TryParseResponse(frame.AsSpan(), out var response) && response.ResponseCode == (byte)expectedCommand)
            {
                await _channel.WriteAsync(new[] { Lcdm4000Protocol.ACK }, ct).ConfigureAwait(false);
                return response;
            }

            await _channel.WriteAsync(new[] { Lcdm4000Protocol.NAK }, ct).ConfigureAwait(false);
            _logger.LogDebug("[LCDM4000] Response validation failed. cmd=0x{Command:X2} attempt={Attempt}", (byte)expectedCommand, attempt);
        }

        throw new InvalidOperationException("LCDM4000 response validation failed.");
    }

    private async Task<byte> TryWaitControlByteAsync(int timeoutMs, CancellationToken ct)
    {
        try
        {
            var frame = await _channel.WaitAsync(
                f => f.Length == 1 && (f.Span[0] == Lcdm4000Protocol.ACK || f.Span[0] == Lcdm4000Protocol.NAK),
                timeoutMs: timeoutMs,
                ct: ct).ConfigureAwait(false);

            return frame.Length == 1 ? frame[0] : Lcdm4000Protocol.NAK;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return Lcdm4000Protocol.NAK;
        }
    }

    public bool TryParseResponse(ReadOnlySpan<byte> frame, out Lcdm4000Response response)
    {
        response = default!;

        if (frame.Length < 6)
            return false;

        if (frame[0] != Lcdm4000Protocol.SOH
            || frame[1] != Lcdm4000Protocol.DeviceId
            || frame[2] != Lcdm4000Protocol.STX)
        {
            return false;
        }

        int etxPos = frame.Length - 2;
        if (frame[etxPos] != Lcdm4000Protocol.ETX)
            return false;

        byte computed = ComputeBcc(frame[..(frame.Length - 1)]);
        if (computed != frame[^1])
            return false;

        byte responseCode = frame[3];
        byte errorByte = frame[4];
        byte[] data = frame.Slice(5, etxPos - 5).ToArray();

        response = new Lcdm4000Response(responseCode, errorByte, data);
        return true;
    }

    private static byte ComputeBcc(ReadOnlySpan<byte> bytes)
    {
        byte bcc = 0;
        for (int i = 0; i < bytes.Length; i++)
            bcc ^= bytes[i];

        return bcc;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
