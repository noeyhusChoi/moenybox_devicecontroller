using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;

namespace DeviceController.Devices.Scanner
{
    public class ScannerClient : IDeviceClient
    {
        private readonly ScannerDeviceConfig _config;
        private SerialPort? _serial;
        private readonly SemaphoreSlim _exchangeLock = new(1, 1);

        public ScannerClient(ScannerDeviceConfig config)
        {
            _config = config;
            ClientId = $"ScannerClient-{config.PortName}";
        }

        public string ClientId { get; }

        public Task<bool> IsConnectedAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_serial?.IsOpen == true);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                _serial?.Close();
            }
            catch
            {
                // swallow; closing errors are non-fatal
            }
            return Task.CompletedTask;
        }

        public async Task<ConnectionResult> ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_serial?.IsOpen == true)
                {
                    return new ConnectionResult(true, "Port already open.");
                }

                _serial = new SerialPort(_config.PortName, _config.BaudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                _serial.Open();
                return new ConnectionResult(true, "Port opened.");
            }
            catch (Exception ex)
            {
                return new ConnectionResult(false, $"Open failed: {ex.Message}");
            }
        }

        public async Task<ClientExchangeResult> ExchangeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
        {
            if (_serial?.IsOpen != true)
            {
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, "Port not open.");
            }

            await _exchangeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stream = _serial.BaseStream;
                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                var frame = await ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false);
                if (frame.Length == 0)
                {
                    return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, "No response.");
                }

                return new ClientExchangeResult(true, frame, "OK");
            }
            catch (OperationCanceledException)
            {
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, "Canceled.");
            }
            catch (TimeoutException ex)
            {
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, $"Timeout: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, $"I/O error: {ex.Message}");
            }
            finally
            {
                _exchangeLock.Release();
            }
        }

        private static async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
        {
            // Length prefix
            var first = await ReadExactAsync(stream, 1, cancellationToken).ConfigureAwait(false);
            if (first.Length == 0) return Array.Empty<byte>();

            int length;
            bool extended = first[0] == 0xFF;
            byte[] header;
            if (extended)
            {
                var lenBytes = await ReadExactAsync(stream, 2, cancellationToken).ConfigureAwait(false);
                length = (lenBytes[0] << 8) | lenBytes[1];
                header = new[] { first[0], lenBytes[0], lenBytes[1] };
            }
            else
            {
                length = first[0];
                header = new[] { first[0] };
            }

            var body = await ReadExactAsync(stream, length, cancellationToken).ConfigureAwait(false);
            var checksum = await ReadExactAsync(stream, 2, cancellationToken).ConfigureAwait(false);

            var frame = new byte[header.Length + body.Length + checksum.Length];
            Buffer.BlockCopy(header, 0, frame, 0, header.Length);
            Buffer.BlockCopy(body, 0, frame, header.Length, body.Length);
            Buffer.BlockCopy(checksum, 0, frame, header.Length + body.Length, checksum.Length);
            return frame;
        }

        private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken cancellationToken)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new TimeoutException("No data available.");
                }
                offset += read;
            }
            return buffer;
        }

        public ValueTask DisposeAsync()
        {
            _serial?.Dispose();
            _exchangeLock.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
