using System;
using System.Linq;
using KIOSK.Domain.Entities;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeResultViewDataProvider : IExchangeResultViewDataProvider
    {
        public ExchangeResultViewData Build(TransactionModelV2 transaction)
        {
            var flagCurrency = transaction.CurrencyPair.BaseCurrency;
            var flagUri = new Uri($"pack://application:,,,/Assets/FLAG/{flagCurrency}.png", UriKind.Absolute);

            return new ExchangeResultViewData
            {
                SelectedCurrency = transaction.CurrencyPair.BaseCurrency,
                SelectedExchangeRate = transaction.CurrencyPair.Rate,
                SelectedCurrencyFlag = flagUri,
                DepositAmount = transaction.SourceDepositedTotal,
                WithdrawalAmount = transaction.TargetComputedAmount,
                WithrawalAmount50000 = transaction.TargetPayouts
                    .Where(x => x.Denomination == 50_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.SucceededCount),
                WithrawalAmount10000 = transaction.TargetPayouts
                    .Where(x => x.Denomination == 10_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.SucceededCount),
                WithrawalAmount5000 = transaction.TargetPayouts
                    .Where(x => x.Denomination == 5_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.SucceededCount),
                WithrawalAmount1000 = transaction.TargetPayouts
                    .Where(x => x.Denomination == 1_000m && x.CurrencyCode.Equals("KRW", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.SucceededCount)
            };
        }
    }
}
