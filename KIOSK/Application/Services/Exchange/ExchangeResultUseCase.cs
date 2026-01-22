using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeResultUseCase : IExchangeResultUseCase
    {
        private readonly IExchangeResultReporter _resultReporter;
        private readonly IExchangeReceiptPrinter _receiptPrinter;

        public ExchangeResultUseCase(
            IExchangeResultReporter resultReporter,
            IExchangeReceiptPrinter receiptPrinter)
        {
            _resultReporter = resultReporter;
            _receiptPrinter = receiptPrinter;
        }

        public async Task RegisterAsync(CancellationToken ct = default)
        {
            await _resultReporter.RegisterAsync(ct);
        }

        public async Task PrintReceiptAsync(bool print, CancellationToken ct = default)
        {
            await _receiptPrinter.PrintReceiptAsync(print, ct);
        }
    }
}
