using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Devices;
using KIOSK.Application.Services.Transactions;
using KIOSK.Infrastructure.OCR;
using KIOSK.Infrastructure.OCR.Models;
using Pr22.Processing;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeIdScanSessionService : IExchangeIdScanSessionService
    {
        private const string IdScannerDeviceId = "IDSCANNER1";
        private readonly IIdScannerPort _scanner;
        private readonly IOcrService _ocr;
        private readonly ITransactionServiceV2 _transaction;

        public ExchangeIdScanSessionService(
            IIdScannerPort scanner,
            IOcrService ocr,
            ITransactionServiceV2 transaction)
        {
            _scanner = scanner;
            _ocr = ocr;
            _transaction = transaction;
        }

        public async Task<bool> ScanAsync(CancellationToken ct)
        {
            var page = await _scanner.SaveImageAsync(IdScannerDeviceId, ct);
            if (page == null)
                return false;

            try
            {
                var outcome = await _ocr.RunAsync(page, OcrMode.Auto, CancellationToken.None);
                if (!outcome.Success)
                    return false;

                await _transaction.UpsertCustomerAsync(
                    outcome.DocumentType,
                    outcome.Fields["NAME"],
                    outcome.Fields["NO"],
                    outcome.Fields["NATIONALITY"]);

                return true;
            }
            finally
            {
                if (page is IDisposable d)
                {
                    try { d.Dispose(); } catch { }
                }
            }
        }
    }
}
