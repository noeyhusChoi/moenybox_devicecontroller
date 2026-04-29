using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels;

public sealed class ExchangeOptionProvider : IExchangeOptionProvider
{
    public IReadOnlyList<CurrencyOptionViewModel> CreateCurrencyOptions(
        Func<string, decimal, Task> selectAsync,
        bool includeKrw = false)
    {
        var options = new List<CurrencyOptionViewModel>();

        if (includeKrw)
            options.Add(Currency("KRW", "1.00", "KOR", selectAsync));

        options.AddRange(
        [
            Currency("USD", "1400.33", "USD", selectAsync),
            Currency("EUR", "1520.11", "EUR", selectAsync),
            Currency("CNY", "193.42", "CNY", selectAsync),
            Currency("TWD", "44.38", "TWD", selectAsync),
            Currency("THB", "38.65", "THB", selectAsync),
            Currency("JPY", "9.34", "JPY", selectAsync),
            Currency("SGD", "1038.25", "SGD", selectAsync),
            Currency("AUD", "912.31", "AUD", selectAsync),
            Currency("GBP", "1788.44", "GBP", selectAsync),
            Currency("PHP", "24.81", "PHP", selectAsync),
            Currency("IDR", "0.086", "IDR", selectAsync),
            Currency("MYR", "312.75", "MYR", selectAsync),
            Currency("VND", "0.055", "VND", selectAsync),
            Currency("THB", "38.65", "THB", selectAsync),
            Currency("CAD", "1012.48", "CAD", selectAsync)
        ]);

        return options;
    }

    private CurrencyOptionViewModel Currency(
        string code,
        string rate,
        string assetCode,
        Func<string, decimal, Task> selectAsync)
        => new(
            code,
            rate,
            $"pack://application:,,,/Assets/Flag/{assetCode}.png",
            new AsyncRelayCommand(() => selectAsync(
                code,
                decimal.Parse(rate, CultureInfo.InvariantCulture))));
}
