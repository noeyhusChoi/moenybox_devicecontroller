using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Models;
using KIOSK.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KIOSK.ViewModels;

public partial class ExchangeCurrencyViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
{
    public Func<Task>? OnStepMain { get; set; }
    public Func<Task>? OnStepPrevious { get; set; }
    public Func<string?, Task>? OnStepNext { get; set; }
    public Action<Exception>? OnStepError { get; set; }

    private readonly IServiceProvider _provider;
    private readonly ITransactionServiceV2 _transactionService;

    [ObservableProperty]
    private ObservableCollection<ExchangeRate> exchangeRates;

    [ObservableProperty]
    private ObservableCollection<Uri> flagUri;

    [ObservableProperty]
    private int _rows = 3;

    public ExchangeCurrencyViewModel(IServiceProvider provider, ITransactionServiceV2 transactionService)
    {
        _provider = provider;
        _transactionService = transactionService;

        var exchangeRateModel = _provider.GetRequiredService<ExchangeRateModel>();
        var excludeExchangeRateList = new[] { "RUB" };      // 제외할 통화 목록 (대소문자 구분 없음)   

        ExchangeRates = new ObservableCollection<ExchangeRate>(
            exchangeRateModel.Data.Where(er => !excludeExchangeRateList.Contains(er.Currency, StringComparer.OrdinalIgnoreCase))
        );
    }

    public async Task OnLoadAsync(object? parameter, CancellationToken ct)
    {
        // TODO: 로딩 시 필요한 작업 수행
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
            if (OnStepError is not null)
                OnStepError(ex);
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
            if (OnStepError is not null)
                OnStepError(ex);
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
                // TODO: BaseCurrency를 설정에서 가져오도록 수정 필요
                //await _transactionService.UpsertCustomerAsync("1", "홍길동", "M12341234", "KR");
                await _transactionService.UpsertRateAsync(new CurrencyPair(param.Currency, param.SpSell ?? 0));
                await _transactionService.UpsertPolicyAsync(param.Currency, "KRW", new ExchangePolicy
                {
                    FeePercent = 0m,
                    FeeFlat = 0m,
                    TargetIncrement = 100m,
                    RoundingMode = RoundingMode.Down
                });

                await _transactionService.NewAsync(param.Currency, "KRW");

                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }
    }
    #endregion
}