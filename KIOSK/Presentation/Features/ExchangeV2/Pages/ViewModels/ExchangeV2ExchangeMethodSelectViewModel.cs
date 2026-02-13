using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Features.ExchangeV2.Orchestration;
using KIOSK.Domain.Transactions;
using KIOSK.Presentation.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2ExchangeMethodSelectViewModel : PageViewModelBase
    {
        private readonly IExchangeV2Orchestrator _orchestrator;

        public ExchangeV2ExchangeMethodSelectViewModel(IExchangeV2Orchestrator orchestrator)
        {
            _orchestrator = orchestrator;
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

        #region Commands

        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private async Task Next(object? parameter)
        {
            if (parameter is not PayoutMethodType method)
            {
                return;
            }

            try
            {
                _orchestrator.SelectPayoutMethod(method);

                await ExecuteStepAsync(OnStepNext, parameter);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }

        #endregion
    }
}
