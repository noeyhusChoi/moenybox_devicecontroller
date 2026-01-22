using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Transactions;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeResultOutboxUpdater : IExchangeResultOutboxUpdater
    {
        private readonly ITransactionOutboxService _outboxService;

        public ExchangeResultOutboxUpdater(ITransactionOutboxService outboxService)
        {
            _outboxService = outboxService;
        }

        public Task MarkSuccessAsync(string transactionId, CancellationToken ct = default)
        {
            return _outboxService.MarkSuccessAsync(transactionId, ct);
        }

        public Task MarkFailAsync(string transactionId, CancellationToken ct = default)
        {
            return _outboxService.MarkFailAsync(transactionId, ct);
        }
    }
}
