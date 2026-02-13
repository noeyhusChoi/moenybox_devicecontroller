using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Features.ExchangeV2.Orchestration;
using KIOSK.Domain.Transactions;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2ExchangeTypeSelectViewModel : PageViewModelBase
    {
        private readonly IExchangeV2Orchestrator _orchestrator;

        public ExchangeV2ExchangeTypeSelectViewModel(IExchangeV2Orchestrator orchestrator)
        {
            _orchestrator = orchestrator;
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
            if (parameter is not ExchangeTransactionType type || type == ExchangeTransactionType.Ready)
            {
                return;
            }

            try
            {
                _orchestrator.SelectTransactionType(type);

                await ExecuteStepAsync(OnStepNext);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }

        #endregion
    }
}
