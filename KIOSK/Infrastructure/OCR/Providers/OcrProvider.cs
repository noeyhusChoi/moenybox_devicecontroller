using KIOSK.Infrastructure.OCR.Models;

namespace KIOSK.Infrastructure.OCR.Providers
{
    public interface IOcrProvider
    {
        Task<OcrOutcome> RunAsync(Pr22.Processing.Page page, CancellationToken ct);
    }
}
