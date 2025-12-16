using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Abstractions;

namespace DeviceController.Devices.Simulated
{
    public class SimulatedDeviceClient : IDeviceClient
    {
        private readonly Random _random = new();
        private bool _connected;

        public string ClientId { get; }

        public SimulatedDeviceClient(string clientId)
        {
            ClientId = clientId;
        }

        public async Task<ConnectionResult> ConnectAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            // Simulate intermittent connection failures.
            var failed = _random.NextDouble() < 0.1;
            _connected = !failed;
            return failed
                ? new ConnectionResult(false, "Simulated link failure.")
                : new ConnectionResult(true, "Connected.");
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
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, "Link not connected.");
            }

            await Task.Delay(80, cancellationToken).ConfigureAwait(false);

            // Simulate occasional timeouts or transport errors.
            var chance = _random.NextDouble();
            if (chance < 0.05)
            {
                _connected = false;
                return new ClientExchangeResult(false, ReadOnlyMemory<byte>.Empty, "Transport error.");
            }

            var echo = Encoding.UTF8.GetBytes($"ACK:{Encoding.UTF8.GetString(payload.Span)}");
            return new ClientExchangeResult(true, echo, "OK");
        }

        public ValueTask DisposeAsync()
        {
            _connected = false;
            return ValueTask.CompletedTask;
        }
    }
}
