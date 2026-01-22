using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeResultOutboxUpdater
    {
        Task MarkSuccessAsync(string transactionId, CancellationToken ct = default);
        Task MarkFailAsync(string transactionId, CancellationToken ct = default);
    }
}
