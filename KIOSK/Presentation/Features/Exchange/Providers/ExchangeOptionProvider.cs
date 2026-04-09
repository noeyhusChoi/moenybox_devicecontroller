using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.Orchestration;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels;

public sealed class ExchangeOptionProvider : IExchangeOptionProvider
{
    private readonly IExchangeFlowCoordinator _coordinator;

    public ExchangeOptionProvider(IExchangeFlowCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public IReadOnlyList<CurrencyOptionViewModel> CreateCurrencyOptions()
        =>
        [
            Currency("USD", "1400.33", "USD"),
            Currency("EUR", "1520.11", "EUR"),
            Currency("CNY", "193.42", "CNY"),
            Currency("TWD", "44.38", "TWD"),
            Currency("THB", "38.65", "THB"),
            Currency("JPY", "9.34", "JPY"),
            Currency("SGD", "1038.25", "SGD"),
            Currency("AUD", "912.31", "AUD"),
            Currency("GBP", "1788.44", "GBP"),
            Currency("PHP", "24.81", "PHP"),
            Currency("IDR", "0.086", "IDR"),
            Currency("MYR", "312.75", "MYR"),
            Currency("VND", "0.055", "VND"),
            Currency("THB", "38.65", "THB"),
            Currency("CAD", "1012.48", "CAD")
        ];

    private CurrencyOptionViewModel Currency(
        string code,
        string rate,
        string assetCode)
        => new(
            code,
            rate,
            $"pack://application:,,,/Assets/Flag/{assetCode}.png",
            new AsyncRelayCommand(() => _coordinator.SelectCurrencyAsync(
                code,
                decimal.Parse(rate, CultureInfo.InvariantCulture))));
}
