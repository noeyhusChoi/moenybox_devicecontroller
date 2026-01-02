using KIOSK.Services.OCR.Models;

namespace KIOSK.Services.OCR.Providers
{
    public interface IOcrProvider
    {
        Task<OcrOutcome> RunAsync(Pr22.Processing.Page page, CancellationToken ct);
    }
}
