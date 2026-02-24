using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Devices
{
    public interface IDepositPort
    {
        event EventHandler<DepositEscrowedEventArgs>? Escrowed;

        Task StartAsync(string deviceId, CancellationToken ct);
        Task StopAsync(string deviceId, CancellationToken ct);
        Task StackAsync(string deviceId, CancellationToken ct);
        Task ReturnAsync(string deviceId, CancellationToken ct);
    }

    public sealed class DepositEscrowedEventArgs : EventArgs
    {
        public DepositEscrowedEventArgs(string deviceId, string payload)
        {
            DeviceId = deviceId;
            Payload = payload;
        }

        public string DeviceId { get; }
        public string Payload { get; }
    }
}
