using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.E200Z;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Device.Drivers;

/// <summary>
/// E200Z 장치 드라이버(정책/상태/이벤트).
/// - 실제 SSI 통신/파싱은 E200ZClient에 위임한다.
/// - 동기 요청-응답 + 비동기 수신(Decoded)을 동시에 처리한다.
/// </summary>
public sealed class QrE200ZDriver : DeviceBase, IQrDriver
{
    private E200ZClient? _client;
    private readonly ILogger<QrE200ZDriver> _logger;

    public event Action<string>? Log;
    public event EventHandler<QrDecodedData>? Decoded;

    public QrE200ZDriver(DeviceDescriptor descriptor, ITransport transport, ILogger<QrE200ZDriver>? logger = null)
        : base(descriptor, transport)
    {
        _logger = logger ?? NullLogger<QrE200ZDriver>.Instance;
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var channel = CreateChannel(new E200ZFramer());
            var client = new E200ZClient(channel);
            client.Log += OnClientLog;
            client.Decoded += OnClientDecoded;
            _client = client;

            await client.StartAsync(ct).ConfigureAwait(false);

            // 초기 설정(실패해도 장치 연결 자체는 유지)
            _ = TryInitSettingsAsync(client, ct);

            return CreateSnapshot();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[E200Z] Initialize error: {ex.Message}");
            await DisposeClientAsync().ConfigureAwait(false);
            _logger.LogError(ex, "E200Z initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        try
        {
            if (_client is null)
                throw new InvalidOperationException("E200Z client not initialized.");

            var result = await _client.RequestRevisionAsync(ct).ConfigureAwait(false);
            if (!result.Success)
                alerts.Add(CreateAlert(new ErrorCode("DEV", "QR", "STATUS", "ERROR"), string.Empty, Severity.Warning));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            alerts.Add(CreateAlert(new ErrorCode("DEV", "QR", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E200Z status failed. device={Device} model={Model}", Name, Model);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        if (command.Name.Equals("RESTART", StringComparison.OrdinalIgnoreCase))
            return new CommandResult(true);

        return CreateUnknownCommandResult();
    }

    public Task<CommandResult> EnableScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(CreateNotConnectedCommandResult());

        return _client.ScanEnableAsync(ct);
    }

    public Task<CommandResult> DisableScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(CreateNotConnectedCommandResult());

        return _client.ScanDisableAsync(ct);
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

        _client.Log -= OnClientLog;
        _client.Decoded -= OnClientDecoded;

        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
    }

    private void OnClientLog(string message) => Log?.Invoke(message);

    private async Task TryInitSettingsAsync(E200ZClient client, CancellationToken ct)
    {
        try
        {
            await client.SetDecodeDataPacketFormatAsync(0x01, true, ct).ConfigureAwait(false); // Packet Mode
            await client.SetAutoInductionTriggerModeAsync(true, ct).ConfigureAwait(false);     // Auto-Induction
            await client.ScanDisableAsync(ct).ConfigureAwait(false);                           // Scan Disable
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[E200Z] Init settings failed: {ex.Message}");
        }
    }
    private void OnClientDecoded(object? sender, DecodeMessage msg)
        => Decoded?.Invoke(this, new QrDecodedData(msg.BarcodeType, msg.Text));

    private CommandResult CreateNotConnectedCommandResult()
    {
        var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceType)
            ? Descriptor.Model
            : Descriptor.DeviceType;

        return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED"));
    }
}
