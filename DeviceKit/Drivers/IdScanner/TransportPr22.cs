using Pr22;

namespace DeviceKit.Drivers.IdScanner;

/// <summary>
/// PR22 DLL 연결 래퍼. DocumentReaderDevice를 열고 닫기만 담당한다.
/// </summary>
internal sealed class TransportPr22 : IAsyncDisposable
{
    private DocumentReaderDevice? _device;
    private bool _isOpen;

    public DocumentReaderDevice Device => _device ?? throw new InvalidOperationException("PR22 device is not opened.");

    public bool IsOpen => _isOpen;

    public Task OpenAsync(CancellationToken ct = default)
    {
        if (_isOpen)
            return Task.CompletedTask;

        _device = new DocumentReaderDevice();
        var list = DocumentReaderDevice.GetDeviceList();
        if (list.Count == 0)
        {
            _device.Dispose();
            _device = null;
            throw new Pr22.Exceptions.NoSuchDevice("No device found.");
        }

        _device.UseDevice(list[0]);
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

    public async ValueTask DisposeAsync()
    {
        try { await CloseAsync().ConfigureAwait(false); } catch { }
    }
}
