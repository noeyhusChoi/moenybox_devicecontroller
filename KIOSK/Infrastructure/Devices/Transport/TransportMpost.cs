using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using MPOST;

namespace KIOSK.Device.Transport;

/// <summary>
/// MPOST DLL 기반 트랜스포트: Acceptor 생성/오픈/클로즈만 담당한다.
/// </summary>
internal sealed class TransportMpost : ITransport
{
    private readonly string _port;
    private Acceptor? _acceptor;
    private bool _isOpen;

    public event EventHandler? Disconnected;

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

        _acceptor = new Acceptor();
        MpostPatcher.Apply(_acceptor.GetType());

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
        catch { }

        try
        {
            if (_acceptor?.Connected == true)
                _acceptor.Close();
        }
        catch { }

        _acceptor = null;
        _isOpen = false;
        return Task.CompletedTask;
    }

    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        => throw new NotSupportedException("MPOST device doesn't use ReadAsync.");

    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        => throw new NotSupportedException("MPOST device doesn't use WriteAsync.");

    public async ValueTask DisposeAsync()
    {
        try { await CloseAsync().ConfigureAwait(false); } catch { }
    }
}
