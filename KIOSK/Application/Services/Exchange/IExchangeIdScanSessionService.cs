using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeIdScanSessionService
    {
        Task<bool> ScanAsync(CancellationToken ct);
    }
}
