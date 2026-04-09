using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MPOST;

namespace DeviceKit.Drivers.Deposit;

/// <summary>
/// MPOST 기반 지폐 투입기 클라이언트. 실제 SDK 호출과 이벤트 처리를 담당한다.
/// </summary>
internal sealed class DepositClient : IAsyncDisposable
{
    private readonly TransportMpost _transport;
    private readonly ILogger _logger;
    private readonly object _presenceLock = new();
    private Acceptor? _billAcceptor;
    private bool _presenceSubscribed;
    private bool _isStack;
    private bool _isReturn;
    private bool _isRejected;

    public event Action<string>? Log;
    public event EventHandler<string>? Escrowed;

    public DepositClient(DeviceDescriptor descriptor, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _logger = logger ?? NullLogger.Instance;
        _transport = new TransportMpost(descriptor.TransportPort);
    }

    public bool Connected => _billAcceptor?.Connected == true;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (!_transport.IsOpen)
        {
            await _transport.OpenAsync(ct).ConfigureAwait(false);
            _billAcceptor = _transport.Acceptor;
            _billAcceptor.OnConnected += HandleConnectedEvent;
            _billAcceptor.OnRejected += HandleRejectedEvent;
            _billAcceptor.OnStacked += HandleStackedEvent;
            _billAcceptor.OnReturned += HandleReturnedEvent;
        }

        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ConnectTimeoutMs);
        while (!Connected && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        if (!Connected)
            throw new TimeoutException("Connect timeout");
    }

    public Task<DeviceCommandResponse> StartAcceptanceAsync()
    {
        EnsureConnected();
        SubscribeEscrow();
        RequireBillAcceptor().EnableAcceptance = true;
        return Task.FromResult(new DeviceCommandResponse(true));
    }

    public Task<DeviceCommandResponse> StopAcceptanceAsync()
    {
        EnsureConnected();
        UnsubscribeEscrow();
        RequireBillAcceptor().EnableAcceptance = false;
        return Task.FromResult(new DeviceCommandResponse(true));
    }

    public async Task<DeviceCommandResponse> StackAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        _isStack = false;
        RequireBillAcceptor().EscrowStack();

        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ActionTimeoutMs);
        while (!_isStack && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        bool ok = _isStack;
        _isStack = false;
        return ok
            ? new DeviceCommandResponse(true)
            : new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "DEPOSIT", "COMMAND", "STACK_FAILED"));
    }

    public async Task<DeviceCommandResponse> ReturnAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        _isReturn = false;
        RequireBillAcceptor().EscrowReturn();

        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ActionTimeoutMs);
        while (!_isReturn && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        bool ok = _isReturn;
        _isReturn = false;
        return ok
            ? new DeviceCommandResponse(true)
            : new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "DEPOSIT", "COMMAND", "RETURN_FAILED"));
    }

    public async Task<DeviceCommandResponse> RejectAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        _isRejected = false;
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ActionTimeoutMs);
        while (!_isRejected && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        bool ok = _isRejected;
        _isRejected = false;
        return ok
            ? new DeviceCommandResponse(true)
            : new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "DEPOSIT", "COMMAND", "REJECT_DETECTED"));
    }

    private void HandleConnectedEvent(object? sender, EventArgs e)
    {
        try
        {
            var billAcceptor = _billAcceptor;
            if (billAcceptor?.Connected == true)
            {
                billAcceptor.EnableAcceptance = false;
                billAcceptor.AutoStack = false;
                Log?.Invoke("[DEPOSIT] Connected");
                _logger.LogInformation("[DEPOSIT] Connected");
            }
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[DEPOSIT] Connect handler error: {ex.Message}");
            _logger.LogWarning(ex, "[DEPOSIT] Connect handler error");
        }
    }

    private void HandleEscrowedEvent(object? sender, EventArgs e)
    {
        try
        {
            var billAcceptor = _billAcceptor;
            if (billAcceptor is null || billAcceptor.DocType != DocumentType.Bill)
                return;

            var doc = billAcceptor.getDocument();
            if (doc == null)
                return;

            Escrowed?.Invoke(this, doc.ValueString);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[DEPOSIT] Escrow handler error: {ex.Message}");
            _logger.LogWarning(ex, "[DEPOSIT] Escrow handler error");
        }
    }

    private void HandleRejectedEvent(object sender, EventArgs e)
    {
        _isRejected = true;
    }

    private void HandleStackedEvent(object sender, EventArgs e)
    {
        _isStack = true;
    }

    private void HandleReturnedEvent(object sender, EventArgs e)
    {
        _isReturn = true;
    }


    private void SubscribeEscrow()
    {
        lock (_presenceLock)
        {
            if (_presenceSubscribed)
                return;

            _presenceSubscribed = true;
            RequireBillAcceptor().OnEscrow += HandleEscrowedEvent;
        }
    }

    private void UnsubscribeEscrow()
    {
        lock (_presenceLock)
        {
            if (!_presenceSubscribed)
                return;

            _presenceSubscribed = false;
            if (_billAcceptor is not null)
                _billAcceptor.OnEscrow -= HandleEscrowedEvent;
        }
    }

    private void CloseAcceptor()
    {
        var billAcceptor = _billAcceptor;
        if (billAcceptor is null)
            return;

        try { billAcceptor.EnableAcceptance = false; } catch { }

        try
        {
            if (billAcceptor.Connected)
                billAcceptor.Close();
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[DEPOSIT] Close error: {ex.Message}");
            _logger.LogWarning(ex, "[DEPOSIT] Close error");
        }
    }

    public async ValueTask DisposeAsync()
    {
        UnsubscribeEscrow();
        if (_billAcceptor is not null)
        {
            _billAcceptor.OnConnected -= HandleConnectedEvent;
            _billAcceptor.OnRejected -= HandleRejectedEvent;
            _billAcceptor.OnStacked -= HandleStackedEvent;
            _billAcceptor.OnReturned -= HandleReturnedEvent;
        }

        CloseAcceptor();
        try { await _transport.DisposeAsync().ConfigureAwait(false); } catch { }
        _billAcceptor = null;
        await Task.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (!Connected)
            throw new InvalidOperationException("Deposit not connected.");
    }

    private Acceptor RequireBillAcceptor()
        => _billAcceptor ?? throw new InvalidOperationException("Deposit not initialized.");
}
