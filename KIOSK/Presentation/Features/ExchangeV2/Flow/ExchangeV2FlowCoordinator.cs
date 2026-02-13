using KIOSK.Application.Abstractions;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Layout.ViewModels;
using KIOSK.Presentation.Abstractions;
using KIOSK.Application.Features.ExchangeV2.StateMachine;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Flow
{
    public sealed class ExchangeV2FlowCoordinator
    {
        private readonly INavigationService _nav;
        private readonly ILoggingService _logging;
        private readonly ExchangeV2StateMachine _state;
        public ExchangeV2FlowCoordinator(
            INavigationService nav,
            ILoggingService logging,
            ExchangeV2StateMachine state)
        {
            _nav = nav;
            _logging = logging;
            _state = state;

            _state.StateEntered += OnStateEnteredAsync;
        }

        public Task StartAsync() => _state.StartAsync();

        private Task OnStateEnteredAsync(ExchangeV2State state) => state switch
        {
            ExchangeV2State.Language => _nav.NavigatePage<ExchangeV2LanguageSelectViewModel>(vm =>
                BindHandlers(vm)),
            ExchangeV2State.Type => _nav.NavigatePage<ExchangeV2ExchangeTypeSelectViewModel>(vm =>
                BindHandlers(vm)),
            ExchangeV2State.Method => _nav.NavigatePage<ExchangeV2ExchangeMethodSelectViewModel>(vm =>
                BindHandlers(vm)),
            ExchangeV2State.Currency => _nav.NavigatePage<ExchangeV2ExchangeCurrencySelectViewModel>(vm =>
                BindHandlers(vm)),
            ExchangeV2State.IdScanConsent => _nav.NavigatePage<ExchangeV2IdScanConsentViewModel>(vm =>
                BindHandlers(vm)),
            ExchangeV2State.IdScanProcess => _nav.NavigatePage<ExchangeV2IdScanProcessViewModel>(vm =>
                BindHandlers(vm)),
            ExchangeV2State.IdScanComplete => _nav.NavigatePage<ExchangeV2IdScanCompleteViewModel>(vm =>
                BindHandlers(vm)),

            ExchangeV2State.Exit => ExitAsync(),
            _ => Task.CompletedTask
        };

        private Task ExitAsync()
        {
            _nav.NavigateLayout<MenuV2LayoutViewModel>();
            return Task.CompletedTask;
        }

        private void BindHandlers(
            PageViewModelBase viewModel,
            bool enableMain = true,
            bool enablePrevious = true,
            bool enableNext = true,
            Func<object?, Task>? nextOverride = null)
        {
            viewModel.OnStepMain = enableMain ? async _ => await _state.ExitAsync() : null;
            viewModel.OnStepPrevious = enablePrevious ? async _ => await _state.PreviousAsync() : null;
            viewModel.OnStepNext = enableNext
                ? nextOverride ?? (async _ => await _state.NextAsync())
                : null;
            viewModel.OnStepError = async ex =>
            {
                _logging.Error(ex, $"OnStepError, {ex.Message}");
                await _state.ErrorAsync();
            };
        }
    }
}
