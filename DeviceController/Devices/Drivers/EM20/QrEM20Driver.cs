using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.EM20;

namespace KIOSK.Device.Drivers;

public sealed class QrEM20Driver : DeviceBase
{
    private Em20Client? _client;

    public QrEM20Driver(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new Em20Client(RequireTransport());
            _client = client;
            await client.StartAsync(ct).ConfigureAwait(false);

            return CreateSnapshot();
        }
        catch (Exception)
        {
            return CreateSnapshot(new[]
            {
                CreateAlarm(new ErrorCode("DEV", "QR", "CONNECT", "FAIL"), string.Empty, Severity.Error)
            });
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alarms = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("EM20 client not initialized.");
            var result = await client.RequestStatusAsync(ct).ConfigureAwait(false);
            if (!result.Success)
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "QR", "TIMEOUT", "RESPONSE"), string.Empty, Severity.Warning));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "QR", "TIMEOUT", "RESPONSE"), string.Empty, Severity.Warning));
        }

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "CONNECT", "FAIL"));

            switch (command)
            {
                case { Name: string name } when name.Equals("RESTART", StringComparison.OrdinalIgnoreCase):
                    return new CommandResult(true);

                case { Name: string name } when name.Equals("SCAN.ONCE", StringComparison.OrdinalIgnoreCase):
                    return await _client.ScanOnceAsync(ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.MANY", StringComparison.OrdinalIgnoreCase):
                    return await _client.ScanManyAsync(count: 3, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.TRIGGERON", StringComparison.OrdinalIgnoreCase):
                    return await _client.TriggerAsync(true, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.TRIGGEROFF", StringComparison.OrdinalIgnoreCase):
                    return await _client.TriggerAsync(false, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN.READ", StringComparison.OrdinalIgnoreCase):
                    return await _client.ReadRawAsync(timeoutMs: 1000, ct).ConfigureAwait(false);

            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "STATUS", "ERROR"));
        }

        return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "QR", "ERROR", "UNKNOWN_COMMAND"));
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeClientAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private Task DisposeClientAsync()
    {
        _client = null;
        return Task.CompletedTask;
    }
}
