using DeviceKit.Drivers.EM20;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceKit.Drivers;

internal sealed class QrEM20Driver : DeviceDriverBase, IQrDriver
{
    public static IReadOnlyDictionary<string, DeviceCommandSpec> CommandTable { get; } =
        new Dictionary<string, DeviceCommandSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["RESTART"] = DeviceCommandSpec.Create<IDeviceDriver>(
                "RESTART",
                "재시작",
                static (_, _, _) => Task.FromResult(new DeviceCommandResponse(true))),
            [Qr.QrCommands.EnableName] = DeviceCommandSpec.Create<IQrDriver>(
                Qr.QrCommands.EnableName,
                "스캔 활성화",
                static (driver, _, ct) => driver.EnableScanAsync(ct)),
            [Qr.QrCommands.DisableName] = DeviceCommandSpec.Create<IQrDriver>(
                Qr.QrCommands.DisableName,
                "스캔 비활성화",
                static (driver, _, ct) => driver.DisableScanAsync(ct)),
            ["SCAN_ONCE"] = DeviceCommandSpec.Create<QrEM20Driver>(
                "SCAN_ONCE",
                "단일 스캔",
                static (driver, _, ct) => driver.GetRequiredClient().ScanOnceAsync(ct)),
        };

    private Em20Client? _client;
    protected override string ErrorTarget => "QR";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    public QrEM20Driver(DeviceDescriptor desc, ILogger<QrEM20Driver>? logger = null)
        : base(desc, logger ?? NullLogger<QrEM20Driver>.Instance)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new Em20Client(Descriptor);
            _client = client;
            await client.StartAsync(ct).ConfigureAwait(false);

            return CreateSnapshot();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            Logger.LogError(ex, "EM20 initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("EM20 client not initialized.");
            var result = await client.RequestStatusAsync(ct).ConfigureAwait(false);
            if (!result.Success)
            {
                alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "ERROR"), result.Message ?? "QR status request failed.", Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning(ex, "EM20 status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "TIMEOUT"), ex.Message, Severity.Warning));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "EM20 status failed. device={Device} model={Model}", Name, Model);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public Task<DeviceCommandResponse> EnableScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.TriggerAsync(true, ct);
    }

    public Task<DeviceCommandResponse> DisableScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.TriggerAsync(false, ct);
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeClientAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private Task DisposeClientAsync()
    {
        if (_client is null)
            return Task.CompletedTask;

        return DisposeClientCoreAsync();
    }

    private Em20Client GetRequiredClient()
        => _client ?? throw new InvalidOperationException("EM20 client not initialized.");

    private async Task DisposeClientCoreAsync()
    {
        var client = _client;
        _client = null;

        try
        {
            if (client is not null)
                await client.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
