using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceKit.Transport;

namespace DeviceKit.Drivers.EM20;

internal sealed class Em20Client : IAsyncDisposable
{
    private readonly TransportChannel _channel;
    private bool _started;

    public Em20Client(DeviceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _channel = new TransportChannel(StreamPortFactory.Create(descriptor), new Em20Framer());
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
        var response = await WaitForResponseAsync(Em20Commands.LedOn, ct).ConfigureAwait(false);
        return response.Length > 0
            ? new DeviceCommandResponse(true)
            : new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "QR", "STATUS", "TIMEOUT"));
    }

    public async Task<DeviceCommandResponse> ReadRawAsync(int timeoutMs, CancellationToken ct = default)
    {
        var response = await WaitForFrameAsync(timeoutMs, ct).ConfigureAwait(false);
        return response.Length > 0
            ? new DeviceCommandResponse(true, "", Encoding.ASCII.GetString(response))
            : new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "QR", "COMMAND", "TIMEOUT"));
    }

    public async Task<DeviceCommandResponse> ScanOnceAsync(CancellationToken ct)
    {
        var val = await ReadLineAsync(ct).ConfigureAwait(false);
        return val is null
            ? new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "QR", "COMMAND", "TIMEOUT"))
            : new DeviceCommandResponse(true, string.Empty, val);
    }

    public async Task<DeviceCommandResponse> ScanManyAsync(int count, CancellationToken ct)
    {
        var results = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var val = await ReadLineAsync(ct).ConfigureAwait(false);
            if (val is null) break;
            results.Add(val);
        }

        return results.Count > 0
            ? new DeviceCommandResponse(true, string.Empty, results)
            : new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "QR", "COMMAND", "TIMEOUT"));
    }

    public async Task<DeviceCommandResponse> TriggerAsync(bool on, CancellationToken ct)
    {
        var cmd = on ? Em20Commands.TriggerOn : Em20Commands.TriggerOff;
        var response = await WaitForResponseAsync(cmd, ct).ConfigureAwait(false);
        if (response.Length == 0)
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "QR", "COMMAND", "TIMEOUT"));

        TraceResponse(cmd, response);

        return new DeviceCommandResponse(true, string.Empty);
    }

    private async Task<string?> ReadLineAsync(CancellationToken ct)
    {
        var response = await WaitForFrameAsync(5000, ct).ConfigureAwait(false);
        if (response.Length == 0)
            return null;

        return Encoding.ASCII.GetString(response).Trim();
    }

    private async Task<byte[]> WaitForResponseAsync(byte[] command, CancellationToken ct)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        try
        {
            return await _channel.SendAndWaitAsync(
                command,
                frame => frame.Length > 0,
                timeoutMs: 5000,
                ct: ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return Array.Empty<byte>();
        }
    }

    private async Task<byte[]> WaitForFrameAsync(int timeoutMs, CancellationToken ct)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        try
        {
            return await _channel.WaitAsync(
                frame => frame.Length > 0,
                timeoutMs: timeoutMs,
                ct: ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return Array.Empty<byte>();
        }
    }

    private Task EnsureStartedAsync(CancellationToken ct)
        => _started ? Task.CompletedTask : StartAsync(ct);

    private static void TraceResponse(byte[] command, byte[] response)
    {
        Trace.WriteLine($"{BitConverter.ToString(command)} | {Encoding.ASCII.GetString(command)}");
        Trace.WriteLine($"{BitConverter.ToString(response)} | {Encoding.ASCII.GetString(response)}");
    }

    public async ValueTask DisposeAsync()
    {
        try { await _channel.DisposeAsync().ConfigureAwait(false); } catch { }
    }

}
