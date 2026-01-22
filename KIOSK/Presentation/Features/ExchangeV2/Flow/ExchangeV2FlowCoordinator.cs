using KIOSK.Application.StateMachines;
using KIOSK.Application.Abstractions;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.Menu.Shell.ViewModels;
using KIOSK.ViewModels;
using System.Windows.Threading;
using KIOSK.Presentation.Features.ExchangeV2.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Shell.ViewModels;

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
            ExchangeV2State.Language => _nav.NavigateTo<ExchangeV2LanguageSelectViewModel>(vm =>
            {
                vm.OnStepMain = async () => await _state.ExitAsync();
                vm.OnStepPrevious = async () => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeV2State.Type => _nav.NavigateTo<ExchangeV2ExchangeTypeSelectViewModel>(vm =>
            {
                vm.OnStepMain = async () => await _state.ExitAsync();
                vm.OnStepPrevious = async () => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeV2State.Method => _nav.NavigateTo<ExchangeV2ExchangeMethodSelectViewModel>(vm =>
            {
                vm.OnStepMain = async () => await _state.ExitAsync();
                vm.OnStepPrevious = async () => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),

            ExchangeV2State.Currency => _nav.NavigateTo<ExchangeV2ExchangeCurrencySelectViewModel>(vm =>
            {
                vm.OnStepMain = async () => await _state.ExitAsync();
                vm.OnStepPrevious = async () => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeV2State.IdScanConsent => _nav.NavigateTo<ExchangeV2IdScanConsentViewModel>(vm =>
            {
                vm.OnStepMain = async () => await _state.ExitAsync();
                vm.OnStepPrevious = async () => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),

            ExchangeV2State.Exit => ExitAsync(),
            _ => Task.CompletedTask
        };

        private Task ExitAsync()
        {
            _nav.SwitchShell<MenuV2ShellViewModel>();
            return Task.CompletedTask;
        }
    }
}
