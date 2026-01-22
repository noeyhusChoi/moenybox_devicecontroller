using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeWithdrawalUseCase
    {
        Task ExecuteAsync(CancellationToken ct);
    }
}
