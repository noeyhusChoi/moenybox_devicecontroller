using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Features.ExchangeV2.Orchestration;
using KIOSK.Presentation.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels;

public partial class ExchangeV2ExchangeCurrencySelectViewModel : PageViewModelBase
{
    private readonly IExchangeV2Orchestrator _orchestrator;
    public ObservableCollection<ExchangeCurrencyItem> Currencies { get; } = new();

    public ExchangeV2ExchangeCurrencySelectViewModel(IExchangeV2Orchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        SeedCurrencies();
    }

    public override Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        // TODO: 로딩 시 필요한 작업 수행
        return Task.CompletedTask;
    }

    public override Task OnUnloadAsync()
    {
        // TODO: 언로드 시 필요한 작업 수행
        return Task.CompletedTask;
    }

    private void SeedCurrencies()
    {
        if (Currencies.Count > 0)
        {
            return;
        }

        Currencies.Add(new ExchangeCurrencyItem("USD", "1455.55", "pack://application:,,,/Assets/Flag/USD.png"));
        Currencies.Add(new ExchangeCurrencyItem("JPY", "975.12", "pack://application:,,,/Assets/Flag/JPY.png"));
        Currencies.Add(new ExchangeCurrencyItem("EUR", "1584.80", "pack://application:,,,/Assets/Flag/EUR.png"));
        Currencies.Add(new ExchangeCurrencyItem("CNY", "200.40", "pack://application:,,,/Assets/Flag/CNY.png"));
        Currencies.Add(new ExchangeCurrencyItem("HKD", "186.22", "pack://application:,,,/Assets/Flag/HKD.png"));
        Currencies.Add(new ExchangeCurrencyItem("TWD", "46.55", "pack://application:,,,/Assets/Flag/TWD.png"));
        Currencies.Add(new ExchangeCurrencyItem("SGD", "1083.90", "pack://application:,,,/Assets/Flag/SGD.png"));
        Currencies.Add(new ExchangeCurrencyItem("THB", "40.88", "pack://application:,,,/Assets/Flag/THB.png"));
        Currencies.Add(new ExchangeCurrencyItem("VND", "0.06", "pack://application:,,,/Assets/Flag/VND.png"));
        Currencies.Add(new ExchangeCurrencyItem("GBP", "1855.31", "pack://application:,,,/Assets/Flag/GBP.png"));
        Currencies.Add(new ExchangeCurrencyItem("CAD", "1070.44", "pack://application:,,,/Assets/Flag/CAD.png"));
        Currencies.Add(new ExchangeCurrencyItem("AUD", "960.50", "pack://application:,,,/Assets/Flag/AUD.png"));
        Currencies.Add(new ExchangeCurrencyItem("NZD", "889.02", "pack://application:,,,/Assets/Flag/NZD.png"));
        Currencies.Add(new ExchangeCurrencyItem("CHF", "1651.90", "pack://application:,,,/Assets/Flag/CHF.png"));
        Currencies.Add(new ExchangeCurrencyItem("AED", "396.30", "pack://application:,,,/Assets/Flag/AED.png"));
    }

    #region Commands

    [RelayCommand]
    private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

    [RelayCommand]
    private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

    [RelayCommand]
    private async Task Next(object? parameter)
    {
        if (parameter is not ExchangeCurrencyItem selected)
        {
            return;
        }

        try
        {
            if (!decimal.TryParse(selected.Rate, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            {
                await RaiseStepErrorAsync(new InvalidOperationException($"Invalid rate for {selected.Code}."));
                return;
            }

            await _orchestrator.ApplyCurrencyAsync(selected.Code, rate);
            await ExecuteStepAsync(OnStepNext, selected.Code);
        }
        catch (System.Exception ex)
        {
            await RaiseStepErrorAsync(ex);
        }
    }

    #endregion
}

public sealed record ExchangeCurrencyItem(string Code, string Rate, string FlagUri);
