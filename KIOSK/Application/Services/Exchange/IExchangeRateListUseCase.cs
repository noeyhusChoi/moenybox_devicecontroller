using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangeRateListUseCase
    {
        Task<IReadOnlyList<ExchangeRate>> LoadAsync(CancellationToken ct = default);
    }
}
