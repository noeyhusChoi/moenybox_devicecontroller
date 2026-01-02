using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;
using MPOST;

namespace KIOSK.Device.Drivers.Deposit;

/// <summary>
/// MPOST 기반 지폐 투입기 클라이언트. 실제 SDK 호출과 이벤트 처리를 담당한다.
/// </summary>
internal sealed class DepositClient : IAsyncDisposable
{
    private readonly TransportMpost _transport;
    private readonly Acceptor _billAcceptor;
    private readonly object _presenceLock = new();
    private bool _presenceSubscribed;
    private bool _isStack;
    private bool _isReturn;
    private bool _isRejected;

    public event Action<string>? Log;
    public event EventHandler<string>? Escrowed;

    public DepositClient(TransportMpost transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _billAcceptor = transport.Acceptor;

        _billAcceptor.OnConnected += HandleConnectedEvent;
        _billAcceptor.OnRejected += HandleRejectedEvent;
        _billAcceptor.OnStacked += HandleStackedEvent;
        _billAcceptor.OnReturned += HandleReturnedEvent;
    }

    public bool Connected => _billAcceptor.Connected;

    public async Task StartAsync(CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ConnectTimeoutMs);
        while (!Connected && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        if (!Connected)
            throw new TimeoutException("Connect timeout");
    }

    public Task<CommandResult> StartAcceptanceAsync()
    {
        EnsureConnected();
        SubscribeEscrow();
        _billAcceptor.EnableAcceptance = true;
        return Task.FromResult(new CommandResult(true));
    }

    public Task<CommandResult> StopAcceptanceAsync()
    {
        EnsureConnected();
        UnsubscribeEscrow();
        _billAcceptor.EnableAcceptance = false;
        return Task.FromResult(new CommandResult(true));
    }

    public async Task<CommandResult> StackAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        _isStack = false;
        _billAcceptor.EscrowStack();

        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ActionTimeoutMs);
        while (!_isStack && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        bool ok = _isStack;
        _isStack = false;
        return ok
            ? new CommandResult(true)
            : new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "CASH", "COMMAND", "STACK_FAIL"));
    }

    public async Task<CommandResult> ReturnAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        _isReturn = false;
        _billAcceptor.EscrowReturn();

        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ActionTimeoutMs);
        while (!_isReturn && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        bool ok = _isReturn;
        _isReturn = false;
        return ok
            ? new CommandResult(true)
            : new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "CASH", "COMMAND", "RETURN_FAIL"));
    }

    public async Task<CommandResult> RejectAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        _isRejected = false;
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(DepositDefaults.ActionTimeoutMs);
        while (!_isRejected && DateTime.UtcNow < deadline)
            await Task.Delay(200, ct).ConfigureAwait(false);

        bool ok = _isRejected;
        _isRejected = false;
        return ok
            ? new CommandResult(true)
            : new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "CASH", "COMMAND", "REJECT_DETECTED"));
    }

    private void HandleConnectedEvent(object? sender, EventArgs e)
    {
        try
        {
            if (_billAcceptor.Connected)
            {
                _billAcceptor.EnableAcceptance = false;
                _billAcceptor.AutoStack = false;
                Log?.Invoke("[DEPOSIT] Connected");
            }
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[DEPOSIT] Connect handler error: {ex.Message}");
        }
    }

    private void HandleEscrowedEvent(object? sender, EventArgs e)
    {
        try
        {
            if (_billAcceptor.DocType != DocumentType.Bill)
                return;

            var doc = _billAcceptor.getDocument();
            if (doc == null)
                return;

            Escrowed?.Invoke(this, doc.ValueString);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[DEPOSIT] Escrow handler error: {ex.Message}");
        }
    }

    private void HandleRejectedEvent(object sender, EventArgs e)
    {
        //Trace.WriteLine($"[OnRejected]");
    }

    private void HandleStackedEvent(object sender, EventArgs e)
    {
        //_isStack = true;
    }

    private void HandleReturnedEvent(object sender, EventArgs e)
    {
        //_isReturn = true;
    }


    private void SubscribeEscrow()
    {
        lock (_presenceLock)
        {
            if (_presenceSubscribed)
                return;

            _presenceSubscribed = true;
            _billAcceptor.OnEscrow += HandleEscrowedEvent;
        }
    }

    private void UnsubscribeEscrow()
    {
        lock (_presenceLock)
        {
            if (!_presenceSubscribed)
                return;

            _presenceSubscribed = false;
            _billAcceptor.OnEscrow -= HandleEscrowedEvent;
        }
    }

    private void CloseAcceptor()
    {
        try { _billAcceptor.EnableAcceptance = false; } catch { }

        try
        {
            if (_billAcceptor.Connected)
                _billAcceptor.Close();
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[DEPOSIT] Close error: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        UnsubscribeEscrow();
        _billAcceptor.OnConnected -= HandleConnectedEvent;
        _billAcceptor.OnRejected -= HandleRejectedEvent;
        _billAcceptor.OnStacked -= HandleStackedEvent;
        _billAcceptor.OnReturned -= HandleReturnedEvent;

        CloseAcceptor();
        await Task.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (!Connected)
            throw new InvalidOperationException("Deposit not connected.");
    }
}
