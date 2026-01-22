using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeDepositUseCase
    {
        event Action<bool>? DepositStateChanged;

        Task StartAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
    }
}
