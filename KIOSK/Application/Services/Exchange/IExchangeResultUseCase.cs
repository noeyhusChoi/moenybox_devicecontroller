using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeResultUseCase
    {
        Task RegisterAsync(CancellationToken ct = default);
        Task PrintReceiptAsync(bool print, CancellationToken ct = default);
    }
}
