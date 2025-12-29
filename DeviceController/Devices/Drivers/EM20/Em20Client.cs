using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;

namespace KIOSK.Device.Drivers.EM20;

internal sealed class Em20Client
{
    private readonly ITransport _transport;
    private bool _started;

    public Em20Client(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started)
            return;

        _started = true;
        if (!_transport.IsOpen)
            await _transport.OpenAsync(ct).ConfigureAwait(false);
    }

    public async Task<CommandResult> RequestStatusAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureOpenAsync(ct).ConfigureAwait(false);
            await _transport.WriteAsync(Em20Commands.LedOn, ct).ConfigureAwait(false);
            await Task.Delay(300, ct).ConfigureAwait(false);

            var resp = new byte[256];
            int read = await _transport.ReadAsync(resp, ct).ConfigureAwait(false);
            return read > 0
                ? new CommandResult(true)
                : new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "TIMEOUT", "RESPONSE"), Retryable: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "STATUS", "ERROR"));
        }
    }

    public async Task<CommandResult> ReadRawAsync(int timeoutMs, CancellationToken ct = default)
    {
        try
        {
            await EnsureOpenAsync(ct).ConfigureAwait(false);
            byte[] resp = new byte[256];
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeoutMs);

            int read = await _transport.ReadAsync(resp, timeoutCts.Token).ConfigureAwait(false);
            return read > 0
                ? new CommandResult(true, "", Encoding.ASCII.GetString(resp, 0, read))
                : new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "TIMEOUT", "RESPONSE"), Retryable: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "STATUS", "ERROR"));
        }
    }

    public async Task<CommandResult> ScanOnceAsync(CancellationToken ct)
    {
        await EnsureOpenAsync(ct).ConfigureAwait(false);
        var val = await ReadLineAsync(ct).ConfigureAwait(false);
        return val is null
            ? new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "TIMEOUT", "RESPONSE"), Retryable: true)
            : new CommandResult(true, string.Empty, val);
    }

    public async Task<CommandResult> ScanManyAsync(int count, CancellationToken ct)
    {
        await EnsureOpenAsync(ct).ConfigureAwait(false);
        var results = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var val = await ReadLineAsync(ct).ConfigureAwait(false);
            if (val is null) break;
            results.Add(val);
        }

        return results.Count > 0
            ? new CommandResult(true, string.Empty, results)
            : new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "TIMEOUT", "RESPONSE"), Retryable: true);
    }

    public async Task<CommandResult> TriggerAsync(bool on, CancellationToken ct)
    {
        try
        {
            await EnsureOpenAsync(ct).ConfigureAwait(false);
            var cmd = on ? Em20Commands.TriggerOn : Em20Commands.TriggerOff;

            await _transport.WriteAsync(cmd, ct).ConfigureAwait(false);
            await Task.Delay(300, ct).ConfigureAwait(false);

            var resp = new byte[256];
            int read = await _transport.ReadAsync(resp, ct).ConfigureAwait(false);
            if (read == 0)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "TIMEOUT", "RESPONSE"), Retryable: true);

            Trace.WriteLine(BitConverter.ToString(cmd));
            Trace.WriteLine(BitConverter.ToString(resp, 0, read));

            return new CommandResult(true, string.Empty);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "STATUS", "ERROR"));
        }
    }

    private Task EnsureOpenAsync(CancellationToken ct)
        => _transport.IsOpen ? Task.CompletedTask : _transport.OpenAsync(ct);

    private async Task<string?> ReadLineAsync(CancellationToken ct)
    {
        var buf = new List<byte>(128);
        var one = new byte[1];
        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

        while (!ct.IsCancellationRequested)
        {
            var readTask = _transport.ReadAsync(one, ct);
            var finished = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

            if (finished == timeoutTask)
                return buf.Count == 0 ? null : Encoding.ASCII.GetString(buf.ToArray()).Trim();

            int n = await readTask.ConfigureAwait(false);
            if (n <= 0)
                continue;

            byte b = one[0];
            if (b is (byte)'\r' or (byte)'\n')
            {
                if (buf.Count == 0)
                {
                    timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
                    continue;
                }

                return Encoding.ASCII.GetString(buf.ToArray()).Trim();
            }

            buf.Add(b);
            timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
        }

        return null;
    }
}
