using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Transport;

/// <summary>
/// 통신이 없는(또는 장치 내부에서만 통신하는) 경우를 위한 No-Op Transport.
/// </summary>
public sealed class TransportNone : ITransport
{
    public event EventHandler? Disconnected;

    public bool IsOpen { get; private set; }

    public Task OpenAsync(CancellationToken ct = default)
    {
        IsOpen = true;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        IsOpen = false;
        SafeRaiseDisconnected();
        return Task.CompletedTask;
    }

    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        => Task.FromResult(0);

    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        IsOpen = false;
        return ValueTask.CompletedTask;
    }

    private void SafeRaiseDisconnected()
    {
        try { Disconnected?.Invoke(this, EventArgs.Empty); } catch { }
    }
}

