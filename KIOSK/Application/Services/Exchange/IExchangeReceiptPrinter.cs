using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeReceiptPrinter
    {
        Task PrintReceiptAsync(bool print, CancellationToken ct = default);
    }
}
