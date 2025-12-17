using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;

namespace KIOSK.Device.Transport;

/// <summary>
/// DLL(객체 기반) 장치 예시용 Transport.
/// - 실제 PR22 SDK가 없는 환경에서도 빌드 가능하도록 스텁으로 유지.
/// </summary>
public sealed class TransportPr22 : ITransport
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
        => throw new NotSupportedException("DLL device doesn't use ReadAsync.");

    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        => throw new NotSupportedException("DLL device doesn't use WriteAsync.");

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

