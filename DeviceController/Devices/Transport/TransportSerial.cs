// Transport/SerialTransport.cs
using KIOSK.Device.Abstractions;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;

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

            // 포트 에러/핀 변경 등 외부 요인(USB 제거 등) 감지
            _port.ErrorReceived += (_, __) => SafeRaiseDisconnected();
            _port.PinChanged += (_, __) =>
            {
                if (!_port.IsOpen) SafeRaiseDisconnected();
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
            try
            {
                if (_port.IsOpen)
                    _port.Close();
            }
            catch
            {
                // close 중 예외가 나도 상위에서 재연결하도록 끊김 이벤트는 보낸다.
            }
            finally
            {
                SafeRaiseDisconnected();
            }

            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
                return 0;

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
                        if (ct.IsCancellationRequested)
                            return 0;

                        return _port.Read(seg.Array, seg.Offset, seg.Count);
                    }
                    catch (TimeoutException)
                    {
                        // 시간 초과
                        //throw;
                        return 0;
                    }
                    catch (OperationCanceledException oce)
                    {
                        SafeRaiseDisconnected();
                        throw new IOException("Serial port read canceled/disconnected.", oce);
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
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 정상 종료
                return 0;
            }
            catch (TimeoutException)
            {
                return 0;
            }
            catch (OperationCanceledException oce)
            {
                SafeRaiseDisconnected();
                throw new IOException("Serial port read canceled/disconnected.", oce);
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
            if (ct.IsCancellationRequested)
                return;

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
                        if (ct.IsCancellationRequested)
                            return;

                        _port.Write(seg.Array, seg.Offset, seg.Count);
                    }
                    catch (TimeoutException)
                    {
                        // 시간 초과
                    }
                    catch (OperationCanceledException oce)
                    {
                        SafeRaiseDisconnected();
                        throw new IOException("Serial port write canceled/disconnected.", oce);
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
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 정상 종료
            }
            catch (OperationCanceledException oce)
            {
                SafeRaiseDisconnected();
                throw new IOException("Serial port write canceled/disconnected.", oce);
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
