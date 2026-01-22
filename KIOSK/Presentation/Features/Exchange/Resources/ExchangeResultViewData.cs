using System;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeResultViewData
    {
        public string SelectedCurrency { get; init; } = string.Empty;
        public decimal SelectedExchangeRate { get; init; }
        public Uri SelectedCurrencyFlag { get; init; } = new Uri("pack://application:,,,/Assets/FLAG/USD.png", UriKind.Absolute);
        public decimal DepositAmount { get; init; }
        public decimal WithdrawalAmount { get; init; }
        public decimal WithrawalAmount50000 { get; init; }
        public decimal WithrawalAmount10000 { get; init; }
        public decimal WithrawalAmount5000 { get; init; }
        public decimal WithrawalAmount1000 { get; init; }
    }
}
