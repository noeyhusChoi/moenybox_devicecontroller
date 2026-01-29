using KIOSK.Application.StateMachines;
using KIOSK.Application.Abstractions;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.Menu.Shell.ViewModels;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels;
using System.Windows.Threading;

namespace KIOSK.Presentation.Features.Exchange.Flow
{
    public sealed class ExchangeFlowCoordinator
    {
        private readonly INavigationService _nav;
        private readonly ILoggingService _logging;
        private readonly ExchangeSellStateMachine _state;

        public ExchangeFlowCoordinator(
            INavigationService nav,
            ILoggingService logging,
            ExchangeSellStateMachine state)
        {
            _nav = nav;
            _logging = logging;
            _state = state;

            _state.StateEntered += OnStateEnteredAsync;
        }

        public Task StartAsync() => _state.StartAsync();

        private Task OnStateEnteredAsync(ExchangeState state) => state switch
        {
            ExchangeState.Language => _nav.NavigatePage<ExchangeLanguageViewModel>(vm =>
            {
                vm.OnStepMain = async _ => await _state.ExitAsync();
                vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Currency => _nav.NavigatePage<ExchangeCurrencyViewModel>(vm =>
            {
                vm.OnStepMain = async _ => await _state.ExitAsync();
                vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Terms => _nav.NavigatePage<ExchangeIDScanConsentViewModel>(vm =>
            {
                vm.OnStepMain = async _ => await _state.ExitAsync();
                vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.IDScan => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigatePage<ExchangeIDScanGuideViewModel>(vm =>
                {
                    vm.OnStepMain = async _ => await _state.ExitAsync();
                    vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                    vm.OnStepNext = async _ => await _state.NextAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.IDScanning => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigatePage<ExchangeIDScanProcessViewModel>(vm =>
                {
                    vm.OnStepMain = async _ => await _state.ExitAsync();
                    vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                    vm.OnStepNext = async _ => await _state.NextAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.IDScanningComplete => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigatePage<ExchangeIDScanCompleteViewModel>(vm =>
                {
                    vm.OnStepMain = async _ => await _state.ExitAsync();
                    vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                    vm.OnStepNext = async _ => await _state.NextAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.Deposit => _nav.NavigatePage<ExchangeDepositViewModel>(vm =>
            {
                vm.OnStepMain = async _ => await _state.ExitAsync();
                vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Withdrawal => _nav.NavigatePage<ExchangeWithdrawalViewModel>(vm =>
            {
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Result => _nav.NavigatePage<ExchangeResultViewModel>(vm =>
            {
                vm.OnStepMain = async _ => await _state.ExitAsync();
                vm.OnStepPrevious = async _ => await _state.PreviousAsync();
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Complete => _nav.NavigatePage<ExchangeCompleteViewModel>(vm =>
            {
                vm.OnStepMain = async _ => await _state.ExitAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Exit => _nav.NavigateLayout<MenuShellViewModel>(),
            _ => Task.CompletedTask
        };
    }
}
