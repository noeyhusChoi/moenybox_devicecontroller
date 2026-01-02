using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.IdScanner;
using KIOSK.Device.Transport;
using Pr22;
using Pr22.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 신분증 스캐너 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 SDK 호출은 IdScannerClient(PR22)에 위임한다.
/// </summary>
public sealed class IdScannerDriver : DeviceBase
{
    private IdScannerClient? _client;
    private CommandDispatcher? _dispatcher;
    private readonly ILogger<IdScannerDriver> _logger;

    public event EventHandler<(int page, Light light, string path)>? ImageSaved;
    public event EventHandler<IdScannerScanEvent>? ScanSequence;
    public event EventHandler? Detected;
    public event Action<string>? Log;

    public IdScannerDriver(DeviceDescriptor desc, ITransport transport, ILogger<IdScannerDriver>? logger = null)
        : base(desc, transport)
    {
        _logger = logger ?? NullLogger<IdScannerDriver>.Instance;
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var pr22 = RequireTransport() as TransportPr22
                ?? throw new InvalidOperationException("IDSCANNER는 PR22 트랜스포트가 필요합니다.");

            var client = new IdScannerClient(pr22);
            client.Log += OnClientLog;
            client.ImageSaved += (_, e) => ImageSaved?.Invoke(this, e);
            client.ScanSequence += (_, e) => ScanSequence?.Invoke(this, e);
            client.Detected += (_, _) => Detected?.Invoke(this, EventArgs.Empty);
            _client = client;
            _dispatcher = new CommandDispatcher(
                IdScannerCommandHandlers.Create(client),
                CreateUnknownCommandResult);
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
            _logger.LogError(ex, "IdScanner initialize failed. device={Device} model={Model}", Name, Model);
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
                alerts.Add(CreateAlert(new ErrorCode("DEV", "IDSCANNER", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "IdScanner status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", "IDSCANNER", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            if (ex is ObjectDisposedException || ex is InvalidOperationException || ex is Pr22.Exceptions.NoSuchDevice)
            {
                _logger.LogWarning(ex, "IdScanner status failed (disconnected). device={Device}", Name);
                await DisposeClientAsync().ConfigureAwait(false);
                throw;
            }
            _logger.LogError(ex, "IdScanner status failed. device={Device}", Name);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;

            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED"));

            if (_dispatcher is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED"));

            return await _dispatcher.DispatchAsync(command, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogWarning(ex, "IdScanner command timeout. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT"), Retryable: true);
        }
        catch (Exception ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogError(ex, "IdScanner command failed. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "ERROR"));
        }
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

        try { _client.Log -= OnClientLog; } catch { }
        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
        _dispatcher = null;
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);
}
