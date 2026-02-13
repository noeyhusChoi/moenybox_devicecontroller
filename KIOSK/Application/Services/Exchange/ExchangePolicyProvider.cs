using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Models;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangePolicyProvider : IExchangePolicyProvider
    {
        public ExchangePolicy GetPolicy(string sourceCurrency, string targetCurrency, ExchangeRate rate)
        {
            return new ExchangePolicy
            {
                FeePercent = 0m,
                FeeFlat = 0m,
                TargetIncrement = 100m,
                RoundingMode = RoundingMode.Down
            };
        }
    }
}
