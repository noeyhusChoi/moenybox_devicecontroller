// Transport/ModbusRtuTransport.cs
using KIOSK.Device.Abstractions;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Device.Transport
{
    /// <summary>
    /// Modbus RTU 전송 (바이트 스트림 I/O)
    /// - 프레임 구성/CRC는 상위(디바이스)에서 처리
    /// - 연결 끊김/에러 시 Disconnected 이벤트 발생
    /// - DeviceSupervisor의 I/O 직렬화와 함께 사용 권장
    /// </summary>
    public sealed class TransportModbusRtu : ITransport
    {
        private readonly SerialPort _port;

        /// <summary>연결 끊김/에러 감지 시 발생</summary>
        public event EventHandler? Disconnected;

        public bool IsOpen => _port.IsOpen;

        /// <param name="portName">예: "COM3"</param>
        /// <param name="baudRate">예: 9600, 19200</param>
        /// <param name="parity">기본: None (장비 사양에 맞게 설정)</param>
        /// <param name="dataBits">기본: 8</param>
        /// <param name="stopBits">기본: One</param>
        /// <param name="rtsEnable">일부 RS-485 컨버터에서 필요</param>
        public TransportModbusRtu(
            string portName,
            int baudRate,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            Parity parity = Parity.None,
            bool rtsEnable = false)
        {
            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                RtsEnable = rtsEnable,
                DtrEnable = false,            // 필요 시 true
                Handshake = Handshake.None    // 필요 시 변경
            };

            // 포트 에러/핀 변경 시 끊김 알림
            _port.ErrorReceived += (_, __) => SafeRaiseDisconnected();
            _port.PinChanged += (_, __) => {
                if (!_port.IsOpen) SafeRaiseDisconnected();
            };
        }

        public Task OpenAsync(CancellationToken ct = default)
        {
            if (!_port.IsOpen) _port.Open();
            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken ct = default)
        {
            try { if (_port.IsOpen) _port.Close(); } catch { /* ignore */ }
            SafeRaiseDisconnected();
            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            try
            {
                // SerialPort.BaseStream.ReadAsync: 0 바이트는 거의 없음(타임아웃/예외로 표현됨)
                return await _port.BaseStream.ReadAsync(buffer, ct).AsTask().ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (TimeoutException) { throw; }
            catch (Exception)
            {
                SafeRaiseDisconnected();
                throw;
            }
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            try
            {
                await _port.BaseStream.WriteAsync(buffer, ct).AsTask().ConfigureAwait(false);
                // Modbus RTU 특성상, 프레임 간 간격(Inter-frame delay)이 필요할 수 있음.
                // 필요 시 상위(Device)에서 Delay 삽입 권장.
            }
            catch (OperationCanceledException) { throw; }
            catch (TimeoutException) { throw; }
            catch (Exception)
            {
                SafeRaiseDisconnected();
                throw;
            }
        }

        public ValueTask DisposeAsync()
        {
            try { _port.Dispose(); } catch { /* ignore */ }
            return ValueTask.CompletedTask;
        }

        private void SafeRaiseDisconnected()
        {
            try { Disconnected?.Invoke(this, EventArgs.Empty); } catch { /* ignore */ }
        }
    }
}
