using System;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Application.Services.Devices;
using KIOSK.Domain.Transactions;
using KIOSK.Infrastructure.OCR;
using KIOSK.Infrastructure.OCR.Models;
using Pr22.Processing;

namespace KIOSK.Application.Features.ExchangeV2.UseCases
{
    public interface IExchangeV2IdScanUseCase
    {
        Task<bool> ProcessAsync(CancellationToken ct);
    }

    public sealed class ExchangeV2IdScanUseCase : IExchangeV2IdScanUseCase
    {
        private static readonly TimeSpan DetectTimeout = TimeSpan.FromSeconds(20);
        private readonly IIdScannerDevice _scanner;
        private readonly IOcrService _ocr;
        private readonly IExchangeV2TransactionContext _tx;

        public ExchangeV2IdScanUseCase(
            IIdScannerDevice scanner,
            IOcrService ocr,
            IExchangeV2TransactionContext tx)
        {
            _scanner = scanner;
            _ocr = ocr;
            _tx = tx;
        }

        public async Task<bool> ProcessAsync(CancellationToken ct)
        {
            Page? page = null;
            var start = await _scanner.ScanStartAsync(ct);
            if (!start.Success)
                return false;

            try
            {
                using var detectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                detectCts.CancelAfter(DetectTimeout);

                var detected = await WaitForDetectedAsync(detectCts.Token);
                if (!detected)
                    return false;

                page = await _scanner.SaveImageAsync(ct);
                if (page is null)
                    return false;

                var outcome = await _ocr.RunAsync(page, OcrMode.Auto, ct);
                if (!outcome.Success)
                    return false;

                var fields = outcome.Fields ?? new System.Collections.Generic.Dictionary<string, string>();

                var idType = outcome.DocumentType ?? string.Empty;
                var customerName = fields.TryGetValue("NAME", out var name) ? name : string.Empty;
                var customerNumber = fields.TryGetValue("NO", out var number) ? number : string.Empty;
                var customerNationality = fields.TryGetValue("NATIONALITY", out var nationality) ? nationality : string.Empty;

                _tx.SetCustomer(new CustomerIdentity
                {
                    IdType = idType,
                    Name = customerName,
                    IdNumber = customerNumber,
                    Nationality = customerNationality,
                    Gender = string.Empty
                });

                return true;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                try
                {
                    await _scanner.ScanStopAsync(CancellationToken.None);
                }
                catch { }

            }
        }

        private async Task<bool> WaitForDetectedAsync(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler handler = (_, _) => tcs.TrySetResult(true);
            _scanner.Detected += handler;
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _scanner.Detected -= handler;
            }
        }
    }
}
