using Kiosk.Infrastructure.OCR.Models;

namespace Kiosk.Infrastructure.OCR.Providers
{
    public interface IOcrProvider
    {
        Task<OcrOutcome> RunAsync(Pr22.Processing.Page page, CancellationToken ct);
    }
}
