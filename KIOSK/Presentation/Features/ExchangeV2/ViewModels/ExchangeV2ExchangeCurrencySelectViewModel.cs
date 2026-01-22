using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Shared.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.ViewModels
{
    public partial class ExchangeV2ExchangeCurrencySelectViewModel : ObservableObject, IStepMain, IStepNext,
        IStepPrevious, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public ObservableCollection<ExchangeCurrencyItem> Currencies { get; } = new();

        public ExchangeV2ExchangeCurrencySelectViewModel()
        {
            SeedCurrencies();
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // TODO: 로딩 시 필요한 작업 수행
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
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
        private async Task Main()
        {
            try
            {
                if (OnStepMain is not null)
                {
                    await OnStepMain();
                }
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
                {
                    await OnStepPrevious();
                }
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        [RelayCommand]
        private async Task Next(object? parameter)
        {
            if (parameter is string param)
            {
                try
                {
                    if (OnStepNext is not null)
                    {
                        await OnStepNext(param);
                    }
                }
                catch (Exception ex)
                {
                    OnStepError?.Invoke(ex);
                }
            }
        }

        #endregion
    }

    public sealed record ExchangeCurrencyItem(string Code, string Rate, string FlagUri);
}
