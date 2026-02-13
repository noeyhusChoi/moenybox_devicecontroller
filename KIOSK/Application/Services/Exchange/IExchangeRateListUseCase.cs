using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Infrastructure.Database.Models;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeRateListUseCase
    {
        Task<IReadOnlyList<ExchangeRate>> LoadAsync(CancellationToken ct = default);
    }
}
