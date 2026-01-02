using KIOSK.Services.OCR.Models;
using KIOSK.Services.OCR.Providers;
using Pr22.Processing;
using System.Diagnostics;

namespace KIOSK.Services.OCR
{
    public interface IOcrService
    {
        Task<OcrOutcome> RunAsync(Page page, OcrMode mode, CancellationToken ct);
    }

    public sealed class OcrService : IOcrService
    {
        private readonly MrzOcrProvider _mrz;
        private readonly ExternalOcrProvider _ext;

        public OcrService(MrzOcrProvider mrz, ExternalOcrProvider ext)
        {
            _mrz = mrz;
            _ext = ext;
        }

        public async Task<OcrOutcome> RunAsync(Page page, OcrMode mode, CancellationToken ct)
        {
            await Task.Delay(1000);

            switch (mode)
            {
                case OcrMode.MrzOnly:
                    return await _mrz.RunAsync(page, ct);

                case OcrMode.ExternalOnly:
                    return await _ext.RunAsync(page, ct);

                case OcrMode.Auto:
                default:
                    // 1) MRZ 시도
                    Trace.WriteLine("MRZ OCR Process");
                    var mrz = await _mrz.RunAsync(page, ct);
                    
                    if (mrz.Success) return mrz;
                    
                    // 2) 외부 OCR 시도
                    Trace.WriteLine("External OCR Process");
                    return await _ext.RunAsync(page, ct);
            }
        }
    }
}
