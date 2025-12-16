using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceController.Core.Commands;
using DeviceController.Core.States;

namespace DeviceController.Core.Abstractions
{
    public record ConnectionResult(bool Success, string? Message = null);

    public record ClientExchangeResult(bool Success, ReadOnlyMemory<byte> Payload, string? Message = null);

    public interface IDeviceClient : IAsyncDisposable
    {
        string ClientId { get; }
        Task<ConnectionResult> ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
        Task<bool> IsConnectedAsync(CancellationToken cancellationToken);
        Task<ClientExchangeResult> ExchangeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
    }
}
