using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Models;

namespace KIOSK.Application.Services.Exchange
{
    public interface IExchangePolicyProvider
    {
        ExchangePolicy GetPolicy(string sourceCurrency, string targetCurrency, ExchangeRate rate);
    }
}
