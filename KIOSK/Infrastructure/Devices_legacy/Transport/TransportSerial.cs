// Transport/SerialTransport.cs
using KIOSK.Device.Abstractions;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace KIOSK.Device.Transport
{
    public sealed class TransportSerial : ITransport
    {
        private readonly SerialPort _port;

        public event EventHandler? Disconnected;

        public TransportSerial(string portName, int baudRate,
            int dataBits = 8, StopBits stopBits = StopBits.One, Parity parity = Parity.None)
        {
            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };
        }

        public bool IsOpen => _port.IsOpen;

        public Task OpenAsync(CancellationToken ct = default)
        {
            try 
            {
                if (!_port.IsOpen)
                {
                    _port.Open(); 
                }
            }
            catch
            {
                SafeRaiseDisconnected();
                throw;
            }

            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken ct = default)
        {
            if (_port.IsOpen)
            {
                _port.Close();
                SafeRaiseDisconnected();
            }

            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            try
            {
                EnsureOpen();
                return await Task.Run(() =>
                {
                    // 바이트 배열만 지원
                    if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> seg) || seg.Array is null)
                        throw new ArgumentException("Buffer must be array-backed.", nameof(buffer));

                    try
                    {
                        return _port.Read(seg.Array, seg.Offset, seg.Count);
                    }
                    catch (TimeoutException tex)
                    {
                        // 시간 초과
                        //throw;
                        return 0;
                    }
                    catch (IOException)
                    {
                        SafeRaiseDisconnected();
                        throw;
                    }
                    catch (InvalidOperationException)
                    {
                        SafeRaiseDisconnected();
                        throw;
                    }
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
                return 0;
            }
            catch (TimeoutException)
            {
                return 0;
            }
            catch (IOException)
            {
                SafeRaiseDisconnected();
                throw;
            }
            catch (InvalidOperationException)
            {
                SafeRaiseDisconnected();
                throw;
            }
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            try
            {
                EnsureOpen();
                await Task.Run(() =>
                {
                    // 바이트 배열만 지원
                    if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> seg) || seg.Array is null)
                        throw new ArgumentException("Buffer must be array-backed.", nameof(buffer));

                    try
                    {
                        _port.Write(seg.Array, seg.Offset, seg.Count);
                    }
                    catch (TimeoutException)
                    {
                        // 시간 초과
                    }
                    catch (IOException)
                    {
                        SafeRaiseDisconnected();
                        throw;
                    }
                    catch (InvalidOperationException)
                    {
                        SafeRaiseDisconnected();
                        throw;
                    }
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            catch (IOException)
            {
                SafeRaiseDisconnected();
                throw;
            }
            catch (InvalidOperationException)
            {
                SafeRaiseDisconnected();
                throw;
            }
        }

        public ValueTask DisposeAsync()
        {
            try { _port.Dispose(); } catch { }
            return ValueTask.CompletedTask;
        }

        private void EnsureOpen()
        {
            if (!_port.IsOpen)
            {
                SafeRaiseDisconnected();
                throw new InvalidOperationException("Serial port is closed.");
            }
        }

        private void SafeRaiseDisconnected()
        {
            try { Disconnected?.Invoke(this, EventArgs.Empty); } catch { }
        }
    }
}
