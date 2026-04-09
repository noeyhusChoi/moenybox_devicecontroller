using MPOST;

namespace DeviceKit.Drivers.Deposit;

/// <summary>
/// MPOST_NEW DLL 기반 연결 래퍼. Acceptor 생성/오픈/클로즈만 담당한다.
/// </summary>
internal sealed class TransportMpost : IAsyncDisposable
{
    private readonly string _port;
    private Acceptor? _acceptor;
    private bool _isOpen;

    public TransportMpost(string port)
    {
        _port = port ?? string.Empty;
    }

    public Acceptor Acceptor => _acceptor ?? throw new InvalidOperationException("MPOST is not opened.");

    public bool IsOpen => _isOpen;

    public Task OpenAsync(CancellationToken ct = default)
    {
        if (_isOpen)
            return Task.CompletedTask;

        _acceptor ??= new Acceptor();

        if (!string.IsNullOrWhiteSpace(_port))
            _acceptor.Open(_port);

        _isOpen = true;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        if (!_isOpen)
            return Task.CompletedTask;

        try
        {
            if (_acceptor is not null)
                _acceptor.EnableAcceptance = false;
        }
        catch
        {
        }

        try
        {
            if (_acceptor is { Connected: true })
                _acceptor.Close();
        }
        catch
        {
        }

        _isOpen = false;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        try { await CloseAsync().ConfigureAwait(false); } catch { }
    }
}
