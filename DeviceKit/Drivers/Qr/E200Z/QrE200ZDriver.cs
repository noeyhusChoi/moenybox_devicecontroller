using DeviceKit.Drivers.E200Z;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace DeviceKit.Drivers;

/// <summary>
/// E200Z 장치 드라이버(정책/상태/이벤트).
/// - 실제 SSI 통신/파싱은 E200ZClient에 위임한다.
/// - 동기 요청-응답 + 비동기 수신(Decoded)을 동시에 처리한다.
/// </summary>
internal sealed class QrE200ZDriver : DeviceDriverBase, IQrDriver
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
            ["START_DECODE"] = DeviceCommandSpec.Create<QrE200ZDriver>(
                "START_DECODE",
                "디코드 시작",
                static (driver, _, ct) => driver.GetRequiredClient().StartDecodeAsync(ct)),
            ["STOP_DECODE"] = DeviceCommandSpec.Create<QrE200ZDriver>(
                "STOP_DECODE",
                "디코드 중지",
                static (driver, _, ct) => driver.GetRequiredClient().StopDecodeAsync(ct)),
            ["RESET"] = DeviceCommandSpec.Create<QrE200ZDriver>(
                "RESET",
                "리셋",
                static (driver, _, ct) => driver.GetRequiredClient().ResetAsync(ct)),
            //["SET_HOST_TRIGGER"] = DeviceCommandSpec.Create<QrE200ZDriver>(
            //    "SET_HOST_TRIGGER",
            //    "Host Trigger 모드",
            //    static (driver, command, ct) => driver.GetRequiredClient().SetHostTriggerModeAsync(ParseSaveToFlash(command.Payload), ct)),
            //["SET_AUTO_TRIGGER"] = DeviceCommandSpec.Create<QrE200ZDriver>(
            //    "SET_AUTO_TRIGGER",
            //    "Auto-Induction 모드",
            //    static (driver, command, ct) => driver.GetRequiredClient().SetAutoInductionTriggerModeAsync(ParseSaveToFlash(command.Payload), ct)),
            //["SET_PACKET_MODE"] = DeviceCommandSpec.Create<QrE200ZDriver>(
            //    "SET_PACKET_MODE",
            //    "Packet 모드",
            //    static (driver, command, ct) => driver.GetRequiredClient().SetDecodeDataPacketFormatAsync(
            //        ParsePacketMode(command.Payload),
            //        ParseSaveToFlash(command.Payload),
            //        ct)),
            ["REQUEST_REVISION"] = DeviceCommandSpec.Create<QrE200ZDriver>(
                "REQUEST_REVISION",
                "Revision 조회",
                static (driver, _, ct) => driver.GetRequiredClient().RequestRevisionAsync(ct)),
        };

    private E200ZClient? _client;
    protected override string ErrorTarget => "QR";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    public event Action<string>? Log;

    public QrE200ZDriver(DeviceDescriptor descriptor, ILogger<QrE200ZDriver>? logger = null)
        : base(descriptor, logger ?? NullLogger<QrE200ZDriver>.Instance)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);

            var client = new E200ZClient(Descriptor);
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
            Logger.LogError(ex, "E200Z initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            if (_client is null)
                throw new InvalidOperationException("E200Z client not initialized.");

            var result = await _client.RequestRevisionAsync(ct).ConfigureAwait(false);
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
            Logger.LogWarning(ex, "E200Z status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "TIMEOUT"), ex.Message, Severity.Warning));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "E200Z status failed. device={Device} model={Model}", Name, Model);
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public Task<DeviceCommandResponse> EnableScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.ScanEnableAsync(ct);
    }

    public Task<DeviceCommandResponse> DisableScanAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

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
    {
        var payload = new QrDecodedPayload(msg.BarcodeType, msg.Text);
        PublishDriverEvent(DeviceEventNames.QrDecoded, payload);
    }

    private E200ZClient GetRequiredClient()
        => _client ?? throw new InvalidOperationException("E200Z client not initialized.");

    private static bool ParseSaveToFlash(object? payload)
    {
        if (payload is null)
            return true;

        if (payload is bool b)
            return b;

        if (payload is string s)
        {
            if (bool.TryParse(s, out var parsedBool))
                return parsedBool;
            if (s == "1")
                return true;
            if (s == "0")
                return false;
        }

        return true;
    }

    private static byte ParsePacketMode(object? payload)
    {
        if (payload is null)
            return 0x01;

        if (payload is byte b)
            return b;

        if (payload is int i && i >= byte.MinValue && i <= byte.MaxValue)
            return (byte)i;

        if (payload is string s)
        {
            var token = s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (byte.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                    return hex;
            }
            else if (byte.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec))
            {
                return dec;
            }
        }

        return 0x01;
    }
}
