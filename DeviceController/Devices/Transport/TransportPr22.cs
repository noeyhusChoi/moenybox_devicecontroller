using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using Pr22;

namespace KIOSK.Device.Transport;

/// <summary>
/// PR22 DLL 기반 트랜스포트: DocumentReaderDevice를 열고 닫기만 담당한다.
/// </summary>
internal class TransportPr22 : ITransport
{
    private DocumentReaderDevice? _device;
    private bool _isOpen;

    public event EventHandler? Disconnected;

    public DocumentReaderDevice Device => _device ?? throw new InvalidOperationException("PR22 기기가 열리지 않았습니다.");

    public bool IsOpen => _isOpen;

    public Task OpenAsync(CancellationToken ct = default)
    {
        if (_isOpen)
            return Task.CompletedTask;

        _device = new DocumentReaderDevice();
        _isOpen = true;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        if (!_isOpen)
            return Task.CompletedTask;

        try { _device?.Close(); } catch { }
        try { _device?.Dispose(); } catch { }

        _device = null;
        _isOpen = false;

        return Task.CompletedTask;
    }

    // DLL 장치는 Read/Write 사용 안 함
    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        => throw new NotSupportedException("DLL device doesn't use ReadAsync.");

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
        => throw new NotSupportedException("DLL device doesn't use WriteAsync.");

    public async ValueTask DisposeAsync()
    {
        try { await CloseAsync().ConfigureAwait(false); } catch { }
    }
}
