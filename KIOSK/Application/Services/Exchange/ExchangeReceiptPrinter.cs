using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Transactions;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeReceiptPrinter : IExchangeReceiptPrinter
    {
        private readonly ITransactionServiceV2 _transactionService;
        private readonly ReceiptPrintService _receiptPrintService;
        private readonly IExchangeReceiptLocaleProvider _localeProvider;

        public ExchangeReceiptPrinter(
            ITransactionServiceV2 transactionService,
            ReceiptPrintService receiptPrintService,
            IExchangeReceiptLocaleProvider localeProvider)
        {
            _transactionService = transactionService;
            _receiptPrintService = receiptPrintService;
            _localeProvider = localeProvider;
        }

        public async Task PrintReceiptAsync(bool print, CancellationToken ct = default)
        {
            if (!print)
                return;

            var locale = _localeProvider.GetLocale();
            await _receiptPrintService.PrintReceiptAsync(locale, _transactionService.Current);
        }
    }
}
