using System.Threading;
using System.Threading.Tasks;
using KIOSK.Infrastructure.Database.Models;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeSelectCurrencyUseCase
    {
        Task SelectAsync(ExchangeRate rate, CancellationToken ct = default);
    }
}
