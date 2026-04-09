using DeviceKit.Drivers.IdScanner;
using DeviceKit.Events.Payloads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pr22.Imaging;

namespace DeviceKit.Drivers;

/// <summary>
/// 신분증 스캐너 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 SDK 호출은 IdScannerClient(PR22)에 위임한다.
/// </summary>
internal sealed class IdScannerDriver : DeviceDriverBase, IIdScannerDriver
{
    public static IReadOnlyDictionary<string, DeviceCommandSpec> CommandTable { get; } =
        new Dictionary<string, DeviceCommandSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["RESTART"] = DeviceCommandSpec.Create<IDeviceDriver>(
                "RESTART",
                "재시작",
                static (_, _, _) => Task.FromResult(new DeviceCommandResponse(true))),
            [IdScanner.IdScannerCommands.ScanStartName] = DeviceCommandSpec.Create<IIdScannerDriver>(
                IdScanner.IdScannerCommands.ScanStartName,
                "스캔 시작",
                static (driver, _, ct) => driver.StartScanAsync(ct)),
            [IdScanner.IdScannerCommands.ScanStopName] = DeviceCommandSpec.Create<IIdScannerDriver>(
                IdScanner.IdScannerCommands.ScanStopName,
                "스캔 중지",
                static (driver, _, ct) => driver.StopScanAsync(ct)),
            [IdScanner.IdScannerCommands.GetScanStatusName] = DeviceCommandSpec.Create<IIdScannerDriver>(
                IdScanner.IdScannerCommands.GetScanStatusName,
                "스캔 상태 조회",
                static (driver, _, ct) => driver.GetScanStatusAsync(ct)),
            [IdScanner.IdScannerCommands.SaveImageName] = DeviceCommandSpec.Create<IIdScannerDriver>(
                IdScanner.IdScannerCommands.SaveImageName,
                "이미지 저장",
                static (driver, _, ct) => driver.SaveImageAsync(ct)),
            ["RUNOCR"] = DeviceCommandSpec.Create<IdScannerDriver>(
                "RUNOCR",
                "MRZ OCR 실행",
                static (driver, command, ct) => driver.GetRequiredClient().RunOcrAsync(command.Payload?.ToString(), ct)),
            ["GETDEVICEID"] = DeviceCommandSpec.Create<IdScannerDriver>(
                "GETDEVICEID",
                "장치 시리얼/ID 조회",
                static (driver, _, ct) => driver.GetRequiredClient().GetDeviceIdAsync(ct)),
        };

    private IdScannerClient? _client;
    protected override string ErrorTarget => "IDSCANNER";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    public event EventHandler<(int page, Light light, string path)>? ImageSaved;

    public IdScannerDriver(DeviceDescriptor desc, ILogger<IdScannerDriver>? logger = null)
        : base(desc, logger ?? NullLogger<IdScannerDriver>.Instance)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);
            var client = new IdScannerClient(Descriptor, Logger);
            client.ImageSaved += OnImageSaved;
            client.DocumentDetected += OnDocumentDetected;
            client.ScanStatusChanged += OnScanStatusChanged;
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
            Logger.LogError(ex, "IdScanner initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("IdScanner not initialized.");

            var status = await client.GetStatusAsync(ct).ConfigureAwait(false);
            if (!status.Success)
            {
                alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "ERROR"), status.Message ?? "IdScanner status request failed.", Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning(ex, "IdScanner status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "TIMEOUT"), ex.Message, Severity.Warning));
        }
        catch (Exception ex)
        {
            if (ex is ObjectDisposedException || ex is InvalidOperationException || ex is Pr22.Exceptions.NoSuchDevice)
            {
                Logger.LogWarning(ex, "IdScanner status failed (disconnected). device={Device}", Name);
                await DisposeClientAsync().ConfigureAwait(false);
                throw;
            }
            Logger.LogError(ex, "IdScanner status failed. device={Device}", Name);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public Task<DeviceCommandResponse> StartScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.StartScanAsync(ct);
    }

    public Task<DeviceCommandResponse> StopScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.StopScanAsync(ct);
    }

    public Task<DeviceCommandResponse> GetScanStatusAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.GetPresenceAsync(ct);
    }

    public Task<DeviceCommandResponse> SaveImageAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.SaveImageAsync(ct);
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeClientAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private async Task DisposeClientAsync()
    {
        if (_client is null)
            return;

        try { _client.ImageSaved -= OnImageSaved; } catch { }
        try { _client.DocumentDetected -= OnDocumentDetected; } catch { }
        try { _client.ScanStatusChanged -= OnScanStatusChanged; } catch { }
        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
    }

    private void OnImageSaved(object? sender, (int page, Light light, string path) e)
    {
        ImageSaved?.Invoke(this, e);
        PublishDriverEvent(
            DeviceEventNames.IdScannerImageSaved,
            new IdScannerImageSavedPayload(
                e.page,
                e.light.ToString(),
                e.path));
    }

    private void OnDocumentDetected()
        => PublishDriverEvent(DeviceEventNames.IdScannerDocumentDetected, new IdScannerDocumentDetectedPayload());

    private void OnScanStatusChanged(object? sender, IdScannerScanStatus status)
        => PublishDriverEvent(
            DeviceEventNames.IdScannerScanStatusChanged,
            new IdScannerScanStatusChangedPayload(status));

    private IdScannerClient GetRequiredClient()
        => _client ?? throw new InvalidOperationException("IdScanner not initialized.");
}
