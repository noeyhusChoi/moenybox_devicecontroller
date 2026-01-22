using KIOSK.Domain.Entities;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public interface IExchangeResultViewDataProvider
    {
        ExchangeResultViewData Build(TransactionModelV2 transaction);
    }
}
