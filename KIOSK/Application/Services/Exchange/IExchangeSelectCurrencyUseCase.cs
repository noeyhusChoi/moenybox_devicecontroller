using System.Threading;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeSelectCurrencyUseCase
    {
        Task SelectAsync(ExchangeRate rate, CancellationToken ct = default);
    }
}
