using System.Threading;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.API.Cems;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeResultSender
    {
        Task<CemsApiResponse> SendAsync(TransactionModelV2 transaction, CancellationToken ct = default);
    }
}
