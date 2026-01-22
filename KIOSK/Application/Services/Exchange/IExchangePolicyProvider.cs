using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangePolicyProvider
    {
        ExchangePolicy GetPolicy(string sourceCurrency, string targetCurrency, ExchangeRate rate);
    }
}
