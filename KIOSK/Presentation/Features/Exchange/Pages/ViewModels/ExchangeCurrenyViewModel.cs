using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.Domain.Entities;
using KIOSK.Presentation.Shared.Abstractions;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels;

public partial class ExchangeCurrencyViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
{
    public Func<Task>? OnStepMain { get; set; }
    public Func<Task>? OnStepPrevious { get; set; }
    public Func<string?, Task>? OnStepNext { get; set; }
    public Action<Exception>? OnStepError { get; set; }

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

    public async Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        var rates = await _rateListUseCase.LoadAsync(ct);
        ExchangeRates = new ObservableCollection<ExchangeRate>(rates);
    }

    public async Task OnUnloadAsync()
    {
        // TODO: 언로드 시 필요한 작업 수행
    }

    #region Commands
    [RelayCommand]
    private async Task Main()
    {
        try
        {
            if (OnStepMain is not null)
                await OnStepMain();
        }
        catch (Exception ex)
        {
            OnStepError?.Invoke(ex);
        }
    }

    [RelayCommand]
    private async Task Previous()
    {
        try
        {
            if (OnStepPrevious is not null)
                await OnStepPrevious();
        }
        catch (Exception ex)
        {
            OnStepError?.Invoke(ex);
        }
    }

    [RelayCommand]
    private async Task Next(object? parameter)
    {
        if (parameter is ExchangeRate param)
        {
            Trace.WriteLine($"target_currency: {param.Currency} = {param.SpSell}");
            try
            {
                await _selectCurrencyUseCase.SelectAsync(param);

                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }
    }
    #endregion
}