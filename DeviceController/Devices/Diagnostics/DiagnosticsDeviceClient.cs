using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;

namespace DeviceController.Devices.Diagnostics
{
    public class DiagnosticsDeviceClient : IDeviceClient
    {
        private readonly Random _random = new();
        private bool _connected;

        public DiagnosticsDeviceClient(string clientId)
        {
            ClientId = clientId;
        }

        public string ClientId { get; }

        public async Task<ConnectionResult> ConnectAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            var ok = _random.NextDouble() > 0.05;
            _connected = ok;
            return ok ? new ConnectionResult(true, "TCP connected.") : new ConnectionResult(false, "TCP handshake failed.");
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            _connected = false;
            return Task.CompletedTask;
        }

        public Task<bool> IsConnectedAsync(CancellationToken cancellationToken) => Task.FromResult(_connected);

        public async Task<ClientExchangeResult> ExchangeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
        {
            if (!_connected)
            {
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, "Not connected.");
            }

            await Task.Delay(120, cancellationToken).ConfigureAwait(false);
            var drop = _random.NextDouble() < 0.08;
            if (drop)
            {
                _connected = false;
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, "Socket dropped.");
            }

            var echo = Encoding.UTF8.GetBytes($"RESP:{Encoding.UTF8.GetString(payload.Span)}");
            return new ClientExchangeResult(true, echo, "OK");
        }

        public ValueTask DisposeAsync()
        {
            _connected = false;
            return ValueTask.CompletedTask;
        }
    }
}
