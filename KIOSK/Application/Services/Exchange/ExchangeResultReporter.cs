using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Transactions;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeResultReporter : IExchangeResultReporter
    {
        private readonly ITransactionServiceV2 _transactionService;
        private readonly IExchangeResultSender _sender;
        private readonly IExchangeResultOutboxUpdater _outboxUpdater;

        public ExchangeResultReporter(
            ITransactionServiceV2 transactionService,
            IExchangeResultSender sender,
            IExchangeResultOutboxUpdater outboxUpdater)
        {
            _transactionService = transactionService;
            _sender = sender;
            _outboxUpdater = outboxUpdater;
        }

        public async Task RegisterAsync(CancellationToken ct = default)
        {
            var transaction = _transactionService.Current;
            var res = await _sender.SendAsync(transaction, ct);
            if (res.Result && res.ECode == null)
                await _outboxUpdater.MarkSuccessAsync(transaction.TransactionID, ct);
            else
                await _outboxUpdater.MarkFailAsync(transaction.TransactionID, ct);
        }
    }
}
