using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.IdScanner;
using KIOSK.Device.Transport;
using Pr22;
using Pr22.Imaging;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 신분증 스캐너 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 SDK 호출은 IdScannerClient(PR22)에 위임한다.
/// </summary>
public sealed class IdScannerDriver : DeviceBase
{
    private IdScannerClient? _client;

    public event EventHandler<(int page, Light light, string path)>? ImageSaved;
    public event EventHandler<IdScannerScanEvent>? ScanSequence;
    public event EventHandler? Detected;
    public event Action<string>? Log;

    public IdScannerDriver(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
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
            await client.StartAsync(ct).ConfigureAwait(false);

            return CreateSnapshot();
        }
        catch (Exception ex)
        {
            await DisposeClientAsync().ConfigureAwait(false);
            return CreateSnapshot(new[]
            {
                CreateAlarm(new ErrorCode("DEV", "IDSCANNER", "CONNECT", "FAIL"), string.Empty, Severity.Error)
            });
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        var alarms = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _client ?? throw new InvalidOperationException("IdScanner not initialized.");

            var status = await client.GetStatusAsync(ct).ConfigureAwait(false);
            if (!status.Success)
            {
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "IDSCANNER", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (ex is ObjectDisposedException || ex is InvalidOperationException || ex is Pr22.Exceptions.NoSuchDevice)
            {
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "IDSCANNER", "CONNECT", "FAIL"), string.Empty, Severity.Error));
                await DisposeClientAsync().ConfigureAwait(false);
            }
            else
            {
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "IDSCANNER", "STATUS", "ERROR"), string.Empty, Severity.Warning));
            }
        }

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "CONNECT", "FAIL"));

            var client = _client;

            switch (command)
            {
                case { Name: string name } when name.Equals("RESTART", StringComparison.OrdinalIgnoreCase):
                    return new CommandResult(true);

                case { Name: string name } when name.Equals("SCANSTART", StringComparison.OrdinalIgnoreCase):
                    return await client.StartScanAsync(ct).ConfigureAwait(false);
                case { Name: string name } when name.Equals("SCANSTOP", StringComparison.OrdinalIgnoreCase):
                    return await client.StopScanAsync(ct).ConfigureAwait(false);
                case { Name: string name } when name.Equals("GETSCANSTATUS", StringComparison.OrdinalIgnoreCase):
                    return await client.GetPresenceAsync(ct).ConfigureAwait(false);
                case { Name: string name } when name.Equals("SAVEIMAGE", StringComparison.OrdinalIgnoreCase):
                    return await client.SaveImageAsync(ct).ConfigureAwait(false);
                default:
                    return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "ERROR", "UNKNOWN_COMMAND"));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "STATUS", "ERROR"));
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
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);
}
