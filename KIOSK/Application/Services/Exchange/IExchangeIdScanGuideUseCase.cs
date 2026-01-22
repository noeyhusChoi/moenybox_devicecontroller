using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeIdScanGuideUseCase
    {
        Task<bool> ScanUntilStableAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
    }
}
