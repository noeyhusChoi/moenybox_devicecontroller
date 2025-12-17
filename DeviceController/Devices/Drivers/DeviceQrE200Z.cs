using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.E200Z;

namespace KIOSK.Device.Drivers;

/// <summary>
/// E200Z 장치 드라이버(정책/상태/이벤트).
/// - 실제 SSI 통신/파싱은 E200ZClient에 위임한다.
/// - 동기 요청-응답 + 비동기 수신(Decoded)을 동시에 처리한다.
/// </summary>
public sealed class DeviceQrE200Z : DeviceBase
{
    private E200ZClient? _client;
    private int _failThreshold;
    private string? _lastRevision;

    public event Action<string>? Log;
    public event EventHandler<DecodeMessage>? Decoded;

    public DeviceQrE200Z(DeviceDescriptor descriptor, ITransport transport)
        : base(descriptor, transport)
    {
    }

    public override async Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);

            await DisposeClientAsync().ConfigureAwait(false);

            var channel = CreateChannel(new E200ZFramer());
            var client = new E200ZClient(channel);
            client.Log += OnClientLog;
            client.Decoded += OnClientDecoded;
            client.RevisionReceived += OnRevisionReceived;
            _client = client;

            await client.StartAsync(ct).ConfigureAwait(false);
            _failThreshold = 0;

            // 초기 설정(실패해도 장치 연결 자체는 유지)
            _ = TryInitSettingsAsync(client, ct);

            return CreateSnapshot();
        }
        catch (Exception ex)
        {
            _failThreshold++;
            Log?.Invoke($"[E200Z] Initialize error: {ex.Message}");
            return CreateSnapshot(new[]
            {
                CreateAlarm("00", "QR 스캐너 초기화 실패", Severity.Error)
            });
        }
    }

    public override async Task<DeviceStatusSnapshot> GetStatusAsync(CancellationToken ct = default, string snapshotId = "")
    {
        var alarms = new List<DeviceAlarm>();

        try
        {
            if (_client is null)
                throw new InvalidOperationException("E200Z client not initialized.");

            var result = await _client.RequestRevisionAsync(ct).ConfigureAwait(false);
            if (!result.Success)
                _failThreshold++;
            else
                _failThreshold = 0;
        }
        catch
        {
            _failThreshold++;
        }

        if (_failThreshold > 5)
            alarms.Add(CreateAlarm("01", "QR 스캐너 통신오류", Severity.Warning));

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        try
        {
            using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

            if (_client is null)
                return new CommandResult(false, "E200Z not connected");

            switch (command)
            {
                case { Name: string name } when name.Equals("SCAN_ENABLE", StringComparison.OrdinalIgnoreCase):
                    return await _client.ScanEnableAsync(ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SCAN_DISABLE", StringComparison.OrdinalIgnoreCase):
                    return await _client.ScanDisableAsync(ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("START_DECODE", StringComparison.OrdinalIgnoreCase):
                    return await _client.StartDecodeAsync(ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("STOP_DECODE", StringComparison.OrdinalIgnoreCase):
                    return await _client.StopDecodeAsync(ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("RESET", StringComparison.OrdinalIgnoreCase):
                    return await _client.ResetAsync(ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SET_HOST_TRIGGER", StringComparison.OrdinalIgnoreCase):
                    return await _client.SetHostTriggerModeAsync(true, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SET_AUTO_TRIGGER", StringComparison.OrdinalIgnoreCase):
                    return await _client.SetAutoInductionTriggerModeAsync(true, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("SET_PACKET_MODE", StringComparison.OrdinalIgnoreCase):
                    return await _client.SetDecodeDataPacketFormatAsync(0x01, true, ct).ConfigureAwait(false);

                case { Name: string name } when name.Equals("REQUEST_REVISION", StringComparison.OrdinalIgnoreCase):
                    {
                        var res = await _client.RequestRevisionAsync(ct).ConfigureAwait(false);
                        return res.Success ? new CommandResult(true, Data: _lastRevision) : res;
                    }
            }

            return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
        }
        catch (OperationCanceledException)
        {
            return new CommandResult(false, $"[{command.Name}] CANCELED COMMAND");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[E200Z] ExecuteAsync error: {ex.Message}");
            return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
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

        _client.Log -= OnClientLog;
        _client.Decoded -= OnClientDecoded;
        _client.RevisionReceived -= OnRevisionReceived;

        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
    }

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

    private void OnClientLog(string message) => Log?.Invoke(message);

    private void OnClientDecoded(object? sender, DecodeMessage msg) => Decoded?.Invoke(this, msg);

    private void OnRevisionReceived(string rev) => _lastRevision = rev;
}

