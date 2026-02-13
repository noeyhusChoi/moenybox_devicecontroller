using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Features.ExchangeV2.Orchestration;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2LanguageSelectViewModel : PageViewModelBase
    {
        private readonly IExchangeV2Orchestrator _orchestrator;

        public ExchangeV2LanguageSelectViewModel(IExchangeV2Orchestrator orchestrator)
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
            if (parameter is not string param)
            {
                return;
            }

            try
            {
                await _orchestrator.SelectLanguageAsync(param);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }

            await ExecuteStepAsync(OnStepNext, param);
        }

        #endregion
    }
}
