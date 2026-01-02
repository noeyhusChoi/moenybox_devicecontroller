using KIOSK.Device.Abstractions;
using MPOST;
using System.Diagnostics;

namespace KIOSK.Device.Drivers;

/// <summary>
/// MPOST 라이브러리 기반 지폐 투입기 장치 드라이버
/// autostack : false (최대한 수동 제어, 절차 지향[화폐검증, 스택/리턴, 기록])
/// </summary>
public sealed class DeviceDeposit : DeviceBase
{
    private readonly Acceptor _billAcceptor = new();
    private readonly object _presenceLock = new();
    private bool _presenceSubscribed;
    private int _failThreshold;

    // MPSOT 전용
    public event EventHandler<string>? OnEscrowed;
    private bool _isStack = false;
    private bool _isReturn = false;
    private bool _isRejected = false;


    public string Port { get; }

    public DeviceDeposit(DeviceDescriptor desc, ITransport transport)
        : base(desc, transport)
    {
        Port = desc.TransportPort;

        MpostPatcher.Apply(_billAcceptor.GetType());

        _billAcceptor.OnConnected += HandleConnectedEvent;
        _billAcceptor.OnRejected += HandleRejectedEvent;
        _billAcceptor.OnStacked += HandleStackedEvent;
        _billAcceptor.OnReturned += HandleReturnedEvent;
    }

    public override async Task<DeviceStatusSnapshot> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_billAcceptor.Connected)
                _billAcceptor.Open(Port);

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);

            while (!_billAcceptor.Connected && DateTime.UtcNow < deadline)
            {
                Trace.WriteLine($"{DateTime.UtcNow} / {deadline}");
                await Task.Delay(TimeSpan.FromMilliseconds(200), ct).ConfigureAwait(false);
            }

            if (!_billAcceptor.Connected)
            {
                _failThreshold++;
                CloseAcceptor();
                return CreateSnapshot(new[]
                {
                    CreateAlarm("DEPOSIT", "연결 대기 시간 초과")
                });
            }

            _failThreshold = 0;
            return CreateSnapshot();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            _failThreshold++;
            CloseAcceptor();
            return CreateSnapshot(new[]
            {
                CreateAlarm("DEPOSIT", "미연결")
            });
        }
    }

    public override async Task<DeviceStatusSnapshot> GetStatusAsync(
        CancellationToken ct = default,
        string snapshotId = "")
    {
        //TODO: 고장 코드 작성 필요
        var alarms = new List<DeviceAlarm>();

        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);
        try
        {
            if (_billAcceptor.Connected)
                _failThreshold = 0;
            else
                _failThreshold++;
        }
        catch
        {
            _failThreshold++;
        }

        if (_failThreshold > 0)
            alarms.Add(CreateAlarm("DEPOSIT", "응답 없음", Severity.Warning));

        return CreateSnapshot(alarms);
    }

    public override async Task<CommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
    {
        using var _ = await AcquireIoAsync(ct).ConfigureAwait(false);

        try
        {

            switch (command)
            {
                case { Name: string name } when name.Equals("START", StringComparison.OrdinalIgnoreCase):
                    return await OnStartDepositAsync();

                case { Name: string name } when name.Equals("STOP", StringComparison.OrdinalIgnoreCase):
                    return await OnStopDepositAsync();

                case { Name: string name } when name.Equals("STACK", StringComparison.OrdinalIgnoreCase):
                    return await StackDepositAsync();

                case { Name: string name } when name.Equals("RETURN", StringComparison.OrdinalIgnoreCase):
                    return await OnReturnDepositAsync();

                default:
                    return new CommandResult(false, $"[{command.Name}] UNKNOWN COMMAND");
            }
        }
        catch (OperationCanceledException)
        {
            return new CommandResult(false, $"[{command.Name}] CANCELED COMMAND");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"[{command.Name}] ERROR COMMAND: {ex.Message}");
        }
    }

    private async Task<CommandResult> OnStartDepositAsync()
    {
        try
        {
            SubscribeEscrow();
            _billAcceptor.EnableAcceptance = true;

            return new CommandResult(true);
        }
        catch
        {
            UnsubscribeEscrow();
            _billAcceptor.EnableAcceptance = false;

            return new CommandResult(false);
        }
    }

    #region Command
    private async Task<CommandResult> OnStopDepositAsync()
    {
        try
        {
            UnsubscribeEscrow();
            _billAcceptor.EnableAcceptance = false;
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return new CommandResult(false);
        }
    }

    private async Task<CommandResult> StackDepositAsync()
    {
        try
        {
            _isStack = false;
            _billAcceptor.EscrowStack();

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

            while (!_isStack && DateTime.UtcNow < deadline)
            {
                //Trace.WriteLine($"{DateTime.UtcNow} / {deadline}");
                await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
            }

            if (_isStack)
            {
                _isStack = false;
                return new CommandResult(true);
            }
            else
            {
                _isStack = false;
                return new CommandResult(false);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return new CommandResult(false);
        }
    }

    private async Task<CommandResult> OnReturnDepositAsync()
    {
        try
        {
            _isReturn = false;
            _billAcceptor.EscrowReturn();

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

            while (!_isReturn && DateTime.UtcNow < deadline)
            {
                //Trace.WriteLine($"{DateTime.UtcNow} / {deadline}");
                await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
            }

            if (_isReturn)
            {
                _isReturn = false;
                return new CommandResult(true);
            }
            else
            {
                _isReturn = false;
                return new CommandResult(false);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return new CommandResult(false);
        }
    }

    private async Task<CommandResult> RejectDeposit()
    {
        try
        {
            _isRejected = false;
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!_isRejected && DateTime.UtcNow < deadline)
            {
                //Trace.WriteLine($"{DateTime.UtcNow} / {deadline}");
                await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
            }
            if (_isRejected)
            {
                _isRejected = false;
                return new CommandResult(true);
            }
            else
            {
                _isRejected = false;
                return new CommandResult(false);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return new CommandResult(false);
        }
    }
    #endregion


    private void HandleConnectedEvent(object? sender, EventArgs e)
    {
        try
        {
            if (_billAcceptor.Connected)
            {
                _billAcceptor.EnableAcceptance = false;
                _billAcceptor.AutoStack = false;
                Trace.WriteLine("Deposit connected");
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
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

            OnEscrowed?.Invoke(this, doc.ValueString);

            Trace.WriteLine($"[OnEscrow] {doc.ValueString}");

        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
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
            Trace.WriteLine($"Deposit close error: {ex.Message}");
        }
    }

    public override async ValueTask DisposeAsync()
    {
        UnsubscribeEscrow();
        _billAcceptor.OnConnected -= HandleConnectedEvent;
        _billAcceptor.OnRejected -= HandleRejectedEvent;
        _billAcceptor.OnStacked -= HandleStackedEvent;
        _billAcceptor.OnReturned -= HandleReturnedEvent;

        CloseAcceptor();
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
