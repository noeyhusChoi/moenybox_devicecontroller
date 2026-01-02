using KIOSK.Device.Abstractions;
using Pr22;
using System.Diagnostics;

namespace KIOSK.Device.Transport
{
    internal class TransportNone : ITransport
    {
        DocumentReaderDevice _dev;
        private bool isOpen = false;

        public event EventHandler? Disconnected;

        public TransportNone()
        {
            _dev = new DocumentReaderDevice();
        }

        public bool IsOpen => isOpen; // 실제 구현 필요

        public Task OpenAsync(CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
        public Task CloseAsync(CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        // DLL 장치는 Read/Write 사용 안 함
        public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
            => throw new NotSupportedException("DLL device doesn't use ReadAsync.");

        public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
            => throw new NotSupportedException("DLL device doesn't use WriteAsync.");

        public ValueTask DisposeAsync()
        {
            try { _dev.Dispose(); } catch { }
            return ValueTask.CompletedTask;
        }
    }
}
