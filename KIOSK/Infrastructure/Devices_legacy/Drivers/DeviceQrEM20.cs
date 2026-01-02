using KIOSK.Device.Abstractions;
using System.Diagnostics;
using System.Text;

namespace KIOSK.Device.Drivers;

public sealed class DeviceQrEM20 : DeviceBase
{
    private int _failThreshold;

    public DeviceQrEM20(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public override async Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            var transport = RequireTransport();

            if (transport.IsOpen)
                await transport.CloseAsync(ct).ConfigureAwait(false);

            await transport.OpenAsync(ct).ConfigureAwait(false);
            _failThreshold = 0;

            return CreateSnapshot();
        }
        catch (Exception)
        {
            _failThreshold++;
            return CreateSnapshot(new[]
            {
                CreateAlarm("QR", "미연결")
            });
        }
    }

    public override async Task<DeviceStatusSnapshot> GetStatusAsync(
        CancellationToken ct = default,
        string temp = "임시메소드")
    {
        var alarms = new List<DeviceAlarm>();

        try
        {
            using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
            var transport = RequireTransport();

            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);

            byte[] cmd = Encoding.ASCII.GetBytes("~\x01" + "0000" + "#" + "LEDONS*" + ";\x03");

            await transport.WriteAsync(cmd, ct).ConfigureAwait(false);

            await Task.Delay(300, ct).ConfigureAwait(false);

            byte[] resp = new byte[256];
            int read = await transport.ReadAsync(resp, ct).ConfigureAwait(false);

            if (read > 0)
            {
                _failThreshold = 0;
            }
            else
            {
                _failThreshold++;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            _failThreshold++;
        }

        if (_failThreshold > 0)
            alarms.Add(CreateAlarm("QR", "응답 없음", Severity.Warning));

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        var transport = RequireTransport();

        try
        {
            switch (command)
            {
                case { Name: string name } when name.Equals("SCAN.ONCE", StringComparison.OrdinalIgnoreCase):
                    return await ScanOnceAsync(transport, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.MANY", StringComparison.OrdinalIgnoreCase):
                    return await ScanManyAsync(transport, count: 3, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.TRIGGERON", StringComparison.OrdinalIgnoreCase):
                    return await SendSoftTriggerAsync(transport, true, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.TRIGGEROFF", StringComparison.OrdinalIgnoreCase):
                    return await SendSoftTriggerAsync(transport, false, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.READ", StringComparison.OrdinalIgnoreCase):
                    {
                        byte[] resp = new byte[256];
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        timeoutCts.CancelAfter(1000);

                        int read = await transport.ReadAsync(resp, timeoutCts.Token).ConfigureAwait(false);
                        return read > 0
                            ? new CommandResult(true, "", Encoding.ASCII.GetString(resp, 0, read))
                            : new CommandResult(false, "No data");
                    }

                default:
                    return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
            }
        }
        catch (OperationCanceledException)
        {
            return new CommandResult(false, $"[{command.Name}] CANCELED COMMAND");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
        }
    }

    private async Task<CommandResult> ScanOnceAsync(ITransport transport, CancellationToken ct)
    {
        var val = await ReadOneAsync(transport, ct).ConfigureAwait(false);
        return val is null
            ? new CommandResult(false, "Timeout")
            : new CommandResult(true, "OK", val);
    }

    private async Task<CommandResult> ScanManyAsync(ITransport transport, int count, CancellationToken ct)
    {
        var results = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var val = await ReadOneAsync(transport, ct).ConfigureAwait(false);
            if (val is null) break;
            results.Add(val);
        }

        return results.Count > 0
            ? new CommandResult(true, "OK", results)
            : new CommandResult(false, "No reads");
    }

    private static readonly byte[] TriggerOnCmd =
        { 0x7E, 0x01, 0x30, 0x30, 0x30, 0x30, 0x23, 0x53, 0x43, 0x4E, 0x45, 0x4E, 0x41, 0x31, 0x3B, 0x03 };

    private static readonly byte[] TriggerOffCmd =
        { 0x7E, 0x01, 0x30, 0x30, 0x30, 0x30, 0x23, 0x53, 0x43, 0x4E, 0x45, 0x4E, 0x41, 0x30, 0x3B, 0x03 };

    private async Task<CommandResult> SendSoftTriggerAsync(ITransport transport, bool on, CancellationToken ct)
    {
        var cmd = on ? TriggerOnCmd : TriggerOffCmd;

        await transport.WriteAsync(cmd, ct).ConfigureAwait(false);

        byte[] resp = new byte[256];

        await Task.Delay(300, ct).ConfigureAwait(false);

        int read = await transport.ReadAsync(resp, ct).ConfigureAwait(false);
        if (read == 0)
            return new CommandResult(false, "No trigger response");

        Trace.WriteLine(BitConverter.ToString(cmd));
        Trace.WriteLine(BitConverter.ToString(resp, 0, read));

        return new CommandResult(true, on ? "Trigger On" : "Trigger Off");
    }

    private async Task<string?> ReadOneAsync(ITransport transport, CancellationToken ct)
    {
        var buf = new List<byte>(128);
        var one = new byte[1];

        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

        while (!ct.IsCancellationRequested)
        {
            var readTask = transport.ReadAsync(one, ct);
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
