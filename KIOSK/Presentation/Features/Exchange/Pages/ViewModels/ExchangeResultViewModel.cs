using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.Exchange;
using KIOSK.Domain.Entities;
using KIOSK.Presentation.Features.Exchange.Resources;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels
{
    public partial class ExchangeResultViewModel : PageViewModelBase
    {

        [ObservableProperty]
        private string testCurrency = "USD";

        [ObservableProperty]
        private decimal testDeposit = 123456;

        [ObservableProperty]
        private decimal test = 12;

        [ObservableProperty]
        private string selectedCurrency;

        [ObservableProperty]
        private decimal selectedExchangeRate;

        [ObservableProperty]
        private Uri selectedCurrencyFlag;

        [ObservableProperty]
        private decimal depositAmount;

        [ObservableProperty]
        private decimal withdrawalAmount;

        [ObservableProperty] private decimal withrawalAmount50000;
        [ObservableProperty] private decimal withrawalAmount10000;
        [ObservableProperty] private decimal withrawalAmount5000;
        [ObservableProperty] private decimal withrawalAmount1000;

        private readonly ITransactionContext _transactionContext;
        private readonly IExchangeResultUseCase _resultUseCase;
        private readonly IExchangeResultViewDataProvider _viewDataProvider;
        public TransactionModelV2 Transaction => _transactionContext.Current;

        public ExchangeResultViewModel(
            ITransactionContext transactionContext,
            IExchangeResultUseCase resultUseCase,
            IExchangeResultViewDataProvider viewDataProvider)
        {
            _transactionContext = transactionContext;
            _resultUseCase = resultUseCase;
            _viewDataProvider = viewDataProvider;

            ApplyViewData(_viewDataProvider.Build(Transaction));
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _resultUseCase.RegisterAsync(ct);
            ApplyViewData(_viewDataProvider.Build(Transaction));
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;

        #region Commands
        [RelayCommand]
        private async Task Next(object? parameter)
        {
            try
            {
                await _resultUseCase.PrintReceiptAsync(parameter is bool print && print, CancellationToken.None);
                await ExecuteStepAsync(OnStepNext, parameter);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }
        #endregion

        private void ApplyViewData(ExchangeResultViewData data)
        {
            SelectedCurrency = data.SelectedCurrency;
            SelectedExchangeRate = data.SelectedExchangeRate;
            SelectedCurrencyFlag = data.SelectedCurrencyFlag;
            DepositAmount = data.DepositAmount;
            WithdrawalAmount = data.WithdrawalAmount;
            WithrawalAmount50000 = data.WithrawalAmount50000;
            WithrawalAmount10000 = data.WithrawalAmount10000;
            WithrawalAmount5000 = data.WithrawalAmount5000;
            WithrawalAmount1000 = data.WithrawalAmount1000;

            Trace.WriteLine($"{WithrawalAmount50000} {WithrawalAmount10000} {WithrawalAmount5000} {WithrawalAmount1000}");
        }
    }
}
