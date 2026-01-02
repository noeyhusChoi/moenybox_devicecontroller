using KIOSK.Device.Abstractions;
using Pr22;
using System.Diagnostics;

namespace KIOSK.Device.Transport
{
    internal class TransportPr22 : ITransport
    {
        DocumentReaderDevice _dev;
        private bool isOpen = false;

        public event EventHandler? Disconnected;

        public TransportPr22()
        {
            _dev = new DocumentReaderDevice();
        }

        public bool IsOpen => isOpen; // 실제 구현 필요

        public Task OpenAsync(CancellationToken ct = default)
        {
            //try
            //{
            //    if (_dev != null)
            //    {
            //        _dev.Close();
            //        _dev.Dispose();
            //    }

            //    var dev = new DocumentReaderDevice();

            //    var list = DocumentReaderDevice.GetDeviceList();
            //    if (list.Count == 0)
            //        throw new Pr22.Exceptions.NoSuchDevice("No device found.");

            //    dev.UseDevice(list[0]);
            //    _dev = dev;

            //    isOpen = true;

            //    Trace.WriteLine("Device connected: " + _dev.DeviceName);
            //}
            //catch (Exception ex)
            //{
            //    isOpen = false;
            //    throw new InvalidOperationException("Failed to open transport", ex);
            //}

            return Task.CompletedTask;
        }
        public Task CloseAsync(CancellationToken ct = default)
        {
            //try
            //{
            //    if (_dev != null) { _dev.Close(); }
            //}
            //catch (Pr22.Exceptions.NoSuchDevice)
            //{
            //    Trace.WriteLine("Pr22 No device Found");
            //}
            //catch (Exception ex)
            //{
            //    Trace.WriteLine($"Pr22 Close Error: {ex.Message}");
            //}
            //finally
            //{
            //    isOpen = false; // 오류 발생 = 통신 안됨 = (isOpen = false)
            //}

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
