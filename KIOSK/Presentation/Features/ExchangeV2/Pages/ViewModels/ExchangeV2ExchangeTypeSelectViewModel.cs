using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.ExchangeV2;
using KIOSK.Domain.Entities;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2ExchangeTypeSelectViewModel : StepViewModelBase
    {
        private readonly IExchangeV2TransactionContext _transactionContext;

        public ExchangeV2ExchangeTypeSelectViewModel(IExchangeV2TransactionContext transactionContext)
        {
            _transactionContext = transactionContext;
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct) => Task.CompletedTask;

        public override Task OnUnloadAsync() => Task.CompletedTask;

        #region Commands

        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private async Task Next(object? parameter)
        {
            if (parameter is not string param)
            {
                return;
            }

            try
            {
                var type = MapTransactionType(param);
                if (type is not null)
                {
                    _transactionContext.SetTransactionType(type.Value);
                }

                await ExecuteStepAsync(OnStepNext);
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        #endregion

        private static ExchangeTransactionType? MapTransactionType(string param)
        {
            if (param.Equals("SELL", StringComparison.OrdinalIgnoreCase))
                return ExchangeTransactionType.SellFX;
            if (param.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                return ExchangeTransactionType.BuyFX;

            return null;
        }
    }
}
