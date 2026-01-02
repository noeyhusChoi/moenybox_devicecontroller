using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers.Deposit;
using KIOSK.Device.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Device.Drivers;

/// <summary>
/// 지폐 투입기 드라이버: 정책/상태/명령 라우팅만 담당하고, 실제 SDK 호출은 DepositClient에 위임한다.
/// </summary>
public sealed class DepositDriver : DeviceBase
{
    private DepositClient? _client;
    private CommandDispatcher? _dispatcher;
    private readonly ILogger<DepositDriver> _logger;

    // MPSOT 전용
    public event EventHandler<string>? OnEscrowed;
    public event Action<string>? Log;

    public DepositDriver(DeviceDescriptor desc, ITransport transport, ILogger<DepositDriver>? logger = null)
        : base(desc, transport)
    {
        _logger = logger ?? NullLogger<DepositDriver>.Instance;
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
            _dispatcher = new CommandDispatcher(
                DepositCommandHandlers.Create(client),
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
            Log?.Invoke($"[DEPOSIT] Initialize error: {ex.Message}");
            _logger.LogError(ex, "Deposit initialize failed. device={Device} model={Model}", Name, Model);
            throw;
        }
    }

    public override async Task<StatusSnapshot> GetStatusAsync(CancellationToken ct = default)
    {
        //TODO: 고장 코드 작성 필요
        var alerts = new List<StatusEvent>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            if (_client is null)
                throw new InvalidOperationException("Deposit not initialized.");

            if (_client.Connected != true)
                alerts.Add(CreateAlert(new ErrorCode("DEV", "CASH", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Deposit status timeout. device={Device}", Name);
            alerts.Add(CreateAlert(new ErrorCode("DEV", "CASH", "STATUS", "TIMEOUT"), string.Empty, Severity.Warning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deposit status failed. device={Device}", Name);
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
            _logger.LogWarning(ex, "Deposit command timeout. device={Device} command={Command}", Name, command.Name);
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT"), Retryable: true);
        }
        catch (Exception ex)
        {
            var deviceKey = string.IsNullOrWhiteSpace(Descriptor.DeviceKey)
                ? Descriptor.Model
                : Descriptor.DeviceKey;
            _logger.LogError(ex, "Deposit command failed. device={Device} command={Command}", Name, command.Name);
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

        try { _client.Escrowed -= OnEscrowedForward; } catch { }
        try { _client.Log -= OnClientLog; } catch { }
        try { await _client.DisposeAsync().ConfigureAwait(false); } catch { }
        _client = null;
        _dispatcher = null;
    }

    private void OnClientLog(string msg) => Log?.Invoke(msg);
    private void OnEscrowedForward(object? sender, string value) => OnEscrowed?.Invoke(this, value);
}
