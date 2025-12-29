using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.Deposit;
using KIOSK.Device.Transport;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 지폐 투입기 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 SDK 호출은 DepositClient에 위임한다.
/// </summary>
public sealed class DepositDriver : DeviceBase
{
    private DepositClient? _client;

    // MPSOT 전용
    public event EventHandler<string>? OnEscrowed;
    public event Action<string>? Log;

    public DepositDriver(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureTransportOpenAsync(ct).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);

            var transport = RequireTransport() as TransportMpost
                ?? throw new InvalidOperationException("DEPOSIT는 MPOST 트랜스포트가 필요합니다.");

            var client = new DepositClient(transport);
            client.Escrowed += OnEscrowedForward;
            client.Log += OnClientLog;
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
            Log?.Invoke($"[DEPOSIT] Initialize error: {ex.Message}");
            return CreateSnapshot(new[]
            {
                CreateAlarm(new ErrorCode("DEV", "CASH", "CONNECT", "FAIL"), string.Empty)
            });
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        //TODO: 고장 코드 작성 필요
        var alarms = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            if (_client?.Connected != true)
                alarms.Add(CreateAlarm(new ErrorCode("DEV", "CASH", "TIMEOUT", "RESPONSE"), string.Empty, Severity.Warning));
        }
        catch
        {
            alarms.Add(CreateAlarm(new ErrorCode("DEV", "CASH", "TIMEOUT", "RESPONSE"), string.Empty, Severity.Warning));
        }

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {
            if (_client is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "CASH", "CONNECT", "FAIL"));

            var client = _client;

            switch (command)
            {
                case { Name: string name } when name.Equals("RESTART", StringComparison.OrdinalIgnoreCase):
                    return new CommandResult(true);

                case { Name: string name } when name.Equals("START", StringComparison.OrdinalIgnoreCase):
                    return await client.StartAcceptanceAsync();

                case { Name: string name } when name.Equals("STOP", StringComparison.OrdinalIgnoreCase):
                    return await client.StopAcceptanceAsync();

                case { Name: string name } when name.Equals("STACK", StringComparison.OrdinalIgnoreCase):
                    return await client.StackAsync(ct);

                case { Name: string name } when name.Equals("RETURN", StringComparison.OrdinalIgnoreCase):
                    return await client.ReturnAsync(ct);

                default:
                    return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "CASH", "ERROR", "UNKNOWN_COMMAND"));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "CASH", "STATUS", "ERROR"));
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

        try { _client.Escrowed -= OnEscrowedForward; } catch { }
        try { _client.Log -= OnClientLog; } catch { }
        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);
    private void OnEscrowedForward(object? sender, string value) => OnEscrowed?.Invoke(this, value);
}
