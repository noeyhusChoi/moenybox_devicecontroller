// Transport/TcpTransport.cs
using KIOSK.Device.Abstractions;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Device.Transport
{
    public sealed class TransportTcp : ITransport
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _client;
        private NetworkStream? _stream;

        public event EventHandler? Disconnected;

        public TransportTcp(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public bool IsOpen => _client?.Connected ?? false;

        public async Task OpenAsync(CancellationToken ct = default)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port, ct).ConfigureAwait(false);
            _stream = _client.GetStream();
        }

        public Task CloseAsync(CancellationToken ct = default)
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            Disconnected?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (_stream is null) throw new InvalidOperationException("Not connected");
            try
            {
                int n = await _stream.ReadAsync(buffer, ct).ConfigureAwait(false);
                //if (n == 0) { Disconnected?.Invoke(this, EventArgs.Empty); }
                return n;
            }
            catch (Exception)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            if (_stream is null) throw new InvalidOperationException("Not connected");
            try
            {
                await _stream.WriteAsync(buffer, ct).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }

        public ValueTask DisposeAsync()
        {
            try { _stream?.Dispose(); } catch { }
            try { _client?.Dispose(); } catch { }
            return ValueTask.CompletedTask;
        }
    }
}
