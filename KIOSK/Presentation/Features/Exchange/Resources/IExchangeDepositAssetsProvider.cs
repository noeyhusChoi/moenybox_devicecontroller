using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public interface IExchangeDepositAssetsProvider
    {
        Task<ExchangeDepositAssets> LoadAsync(string currencyCode, CancellationToken ct = default);
    }
}
