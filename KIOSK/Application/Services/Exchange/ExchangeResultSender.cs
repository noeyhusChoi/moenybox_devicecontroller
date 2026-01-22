using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.API;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.API.Cems;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeResultSender : IExchangeResultSender
    {
        private readonly CemsApiService _cemsApiService;

        public ExchangeResultSender(CemsApiService cemsApiService)
        {
            _cemsApiService = cemsApiService;
        }

        public Task<CemsApiResponse> SendAsync(TransactionModelV2 transaction, CancellationToken ct = default)
        {
            return _cemsApiService.RegisterTransactionAsync(transaction, ct);
        }
    }
}
