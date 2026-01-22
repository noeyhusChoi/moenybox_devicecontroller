using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeDispenseSessionService
    {
        Task ExecuteAsync(CancellationToken ct);
    }
}
