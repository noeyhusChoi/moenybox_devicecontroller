using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using DeviceController.Core.Abstractions;
using System.Text;

namespace DeviceController.Devices.Scanner
{
    public class ScannerClient : IDeviceClient
    {
        private readonly ScannerDeviceConfig _config;
        private readonly Services.IDecodeEventBus? _decodeBus;
        private SerialPort? _serial;
        private readonly SemaphoreSlim _exchangeLock = new(1, 1);
        private readonly Channel<ReadOnlyMemory<byte>> _responses = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        private CancellationTokenSource? _cts;
        private Task? _receiveLoop;

        public ScannerClient(ScannerDeviceConfig config, Services.IDecodeEventBus? decodeBus = null)
        {
            _config = config;
            _decodeBus = decodeBus;
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
            _cts?.Cancel();
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
                StartReceiveLoop();
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
                while (_responses.Reader.TryRead(out _)) { }

                var stream = _serial.BaseStream;
                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                var frame = await _responses.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
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
            var first = await ReadExactAsync(stream, 1, cancellationToken).ConfigureAwait(false);
            if (first.Length == 0) return Array.Empty<byte>();

            bool extended = first[0] == 0xFF;
            int bodyLength;
            byte[] header;
            if (extended)
            {
                var lenBytes = await ReadExactAsync(stream, 2, cancellationToken).ConfigureAwait(false);
                var lenValue = (lenBytes[0] << 8) | lenBytes[1]; // includes LenH/LenL but excludes checksum
                bodyLength = lenValue - 2; // minus LenH/LenL already read
                header = new[] { first[0], lenBytes[0], lenBytes[1] };
            }
            else
            {
                var lenValue = first[0]; // includes Length byte itself
                bodyLength = lenValue - 1; // minus the length byte already read
                header = new[] { first[0] };
            }

            if (bodyLength < 0)
            {
                return Array.Empty<byte>();
            }

            var body = await ReadExactAsync(stream, bodyLength, cancellationToken).ConfigureAwait(false);
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
            _cts?.Cancel();
            _serial?.Dispose();
            _exchangeLock.Dispose();
            return ValueTask.CompletedTask;
        }

        private void StartReceiveLoop()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _receiveLoop = Task.Run(async () =>
            {
                if (_serial?.BaseStream is not Stream stream) return;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var frame = await ReadFrameAsync(stream, token).ConfigureAwait(false);
                        if (frame.Length == 0) continue;
                        if (!TryParseFrame(frame, out var opcode, out var data, out _))
                        {
                            continue;
                        }

                        if (opcode == 0xF4)
                        {
                            var decode = ParseDecode(data);
                            if (decode != null)
                            {
                                _decodeBus?.Publish(decode);
                            }
                            continue;
                        }

                        if (IsResponseOpcode(opcode))
                        {
                            await _responses.Writer.WriteAsync(frame, token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // ignore and continue
                    }
                }
            }, token);
        }

        private static bool TryParseFrame(ReadOnlySpan<byte> frame, out byte opcode, out ReadOnlySpan<byte> data, out byte status)
        {
            opcode = 0;
            data = ReadOnlySpan<byte>.Empty;
            status = 0;
            if (frame.Length < 5) return false;

            int offset;
            int bodyLength;
            if (frame[0] == 0xFF)
            {
                if (frame.Length < 6) return false;
                var lenValue = (frame[1] << 8) | frame[2];
                bodyLength = lenValue - 2;
                offset = 3;
            }
            else
            {
                var lenValue = frame[0];
                bodyLength = lenValue - 1;
                offset = 1;
            }

            if (bodyLength < 3 || offset + bodyLength > frame.Length - 2)
            {
                return false;
            }

            if (!VerifyChecksum(frame))
            {
                return false;
            }

            var payload = frame.Slice(offset, bodyLength);
            opcode = payload[0];
            status = payload[2];
            data = payload.Length > 3 ? payload.Slice(3) : ReadOnlySpan<byte>.Empty;
            return true;
        }

        private static ScannerDecodeData? ParseDecode(ReadOnlySpan<byte> data)
        {
            if (data.Length < 2) return null;
            var type = data[0];
            var payload = Encoding.ASCII.GetString(data.Slice(1));
            return new ScannerDecodeData(type, payload);
        }

        private static bool IsResponseOpcode(byte opcode) =>
            opcode == 0xD0 // ACK
            || opcode == 0xD1 // NAK
            || opcode == 0xA4; // Revision response

        private static bool VerifyChecksum(ReadOnlySpan<byte> frame)
        {
            if (frame.Length < 3) return false;
            var data = frame[..^2];
            var provided = frame[^2..];
            var (h, l) = ComputeChecksum(data);
            return h == provided[0] && l == provided[1];
        }

        private static (byte high, byte low) ComputeChecksum(ReadOnlySpan<byte> data)
        {
            int sum = 0;
            foreach (var b in data)
            {
                sum += b;
            }
            var checksum = (ushort)((0x10000 - (sum & 0xFFFF)) & 0xFFFF);
            return ((byte)((checksum >> 8) & 0xFF), (byte)(checksum & 0xFF));
        }
    }
}
