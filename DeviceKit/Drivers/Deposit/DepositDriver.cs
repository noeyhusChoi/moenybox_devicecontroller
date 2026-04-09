using DeviceKit.Drivers.Deposit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceKit.Drivers;

/// <summary>
/// 지폐 투입기 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 SDK 호출은 DepositClient에 위임한다.
/// </summary>
internal sealed class DepositDriver : DeviceDriverBase, IDepositDriver
{
    public static IReadOnlyDictionary<string, DeviceCommandSpec> CommandTable { get; } =
        new Dictionary<string, DeviceCommandSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["RESTART"] = DeviceCommandSpec.Create<IDeviceDriver>(
                "RESTART",
                "재시작",
                static (_, _, _) => Task.FromResult(new DeviceCommandResponse(true))),
            [Deposit.DepositCommands.StartName] = DeviceCommandSpec.Create<IDepositDriver>(
                Deposit.DepositCommands.StartName,
                "입금 시작",
                static (driver, _, ct) => driver.StartAcceptanceAsync(ct)),
            [Deposit.DepositCommands.StopName] = DeviceCommandSpec.Create<IDepositDriver>(
                Deposit.DepositCommands.StopName,
                "입금 중지",
                static (driver, _, ct) => driver.StopAcceptanceAsync(ct)),
            [Deposit.DepositCommands.StackName] = DeviceCommandSpec.Create<IDepositDriver>(
                Deposit.DepositCommands.StackName,
                "스택 처리",
                static (driver, _, ct) => driver.StackAsync(ct)),
            [Deposit.DepositCommands.ReturnName] = DeviceCommandSpec.Create<IDepositDriver>(
                Deposit.DepositCommands.ReturnName,
                "리턴 처리",
                static (driver, _, ct) => driver.ReturnAsync(ct)),
        };

    private DepositClient? _client;
    protected override string ErrorTarget => "DEPOSIT";
    protected override IReadOnlyDictionary<string, DeviceCommandSpec> Commands => CommandTable;
    protected override bool IsCommandReady => _client is not null;

    // MPSOT 전용
    public event Action<string>? Log;

    public DepositDriver(DeviceDescriptor desc, ILogger<DepositDriver>? logger = null)
        : base(desc, logger ?? NullLogger<DepositDriver>.Instance)
    {
    }

    public override async Task<StatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await DisposeClientAsync().ConfigureAwait(false);
            var client = new DepositClient(Descriptor, Logger);
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
            Logger.LogError(ex, "Deposit initialize failed. device={Device} model={Model}", Name, Model);
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
                throw new InvalidOperationException("Deposit not initialized.");

            if (_client.Connected != true)
                alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "ERROR"), "Deposit device is not connected.", Severity.Warning));
        }
        catch (TimeoutException ex)
        {
            Logger.LogWarning(ex, "Deposit status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "TIMEOUT"), ex.Message, Severity.Warning));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Deposit status failed. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", ErrorTarget, "STATUS", "ERROR"), ex.Message, Severity.Warning));
            throw;
        }

        return CreateSnapshot(alerts);
    }

    public Task<DeviceCommandResponse> StartAcceptanceAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.StartAcceptanceAsync();
    }

    public Task<DeviceCommandResponse> StopAcceptanceAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.StopAcceptanceAsync();
    }

    public Task<DeviceCommandResponse> StackAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.StackAsync(ct);
    }

    public Task<DeviceCommandResponse> ReturnAsync(CancellationToken ct = default)
    {
        if (_client is null)
            return Task.FromResult(new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", ErrorTarget, "COMMAND", "NOT_CONNECTED")));

        return _client.ReturnAsync(ct);
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
    private void OnEscrowedForward(object? sender, string value)
    {
        PublishDriverEvent(DeviceEventNames.DepositEscrowed, new DepositEscrowedPayload(value));
    }

    private DepositClient GetRequiredClient()
        => _client ?? throw new InvalidOperationException("Deposit not initialized.");
}
