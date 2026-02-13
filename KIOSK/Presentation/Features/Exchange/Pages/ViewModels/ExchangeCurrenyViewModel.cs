using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.Infrastructure.Database.Models;
using KIOSK.Presentation.Abstractions;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels;

public partial class ExchangeCurrencyViewModel : PageViewModelBase
{

    private readonly IExchangeSelectCurrencyUseCase _selectCurrencyUseCase;
    private readonly IExchangeRateListUseCase _rateListUseCase;

    [ObservableProperty]
    private ObservableCollection<ExchangeRate> exchangeRates;

    [ObservableProperty]
    private ObservableCollection<Uri> flagUri;

    [ObservableProperty]
    private int rows = 3;

    public ExchangeCurrencyViewModel(IExchangeSelectCurrencyUseCase selectCurrencyUseCase, IExchangeRateListUseCase rateListUseCase)
    {
        _selectCurrencyUseCase = selectCurrencyUseCase;
        _rateListUseCase = rateListUseCase;
        ExchangeRates = new ObservableCollection<ExchangeRate>();
    }

    public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        var rates = await _rateListUseCase.LoadAsync(ct);
        ExchangeRates = new ObservableCollection<ExchangeRate>(rates);
    }

    public override Task OnUnloadAsync() => Task.CompletedTask;

    #region Commands
    [RelayCommand]
    private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

    [RelayCommand]
    private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

    [RelayCommand]
    private async Task Next(object? parameter)
    {
        if (parameter is not ExchangeRate selected)
        {
            return;
        }

        Trace.WriteLine($"target_currency: {selected.Currency} = {selected.SpSell}");
      
        try
        {
            await _selectCurrencyUseCase.SelectAsync(selected);
        }
        catch (Exception ex)
        {
            await RaiseStepErrorAsync(ex);
            return;
        }

        await ExecuteStepAsync(OnStepNext, selected.Currency);
    }
    #endregion
}
