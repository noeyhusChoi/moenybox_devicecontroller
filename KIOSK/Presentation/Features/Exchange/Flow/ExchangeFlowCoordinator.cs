using KIOSK.Application.StateMachines;
using KIOSK.Infrastructure.Logging;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Presentation.Features.Menu.Shell.ViewModels;
using KIOSK.ViewModels;
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
            ExchangeState.Language => _nav.NavigateTo<ExchangeLanguageViewModel>(vm =>
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
            ExchangeState.Currency => _nav.NavigateTo<ExchangeCurrencyViewModel>(vm =>
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
            ExchangeState.Terms => _nav.NavigateTo<ExchangeIDScanConsentViewModel>(vm =>
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
            ExchangeState.IDScan => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigateTo<ExchangeIDScanGuideViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepPrevious = async () => await _state.PreviousAsync();
                    vm.OnStepNext = async _ => await _state.NextAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.IDScanning => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigateTo<ExchangeIDScanProcessViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepPrevious = async () => await _state.PreviousAsync();
                    vm.OnStepNext = async _ => await _state.NextAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.IDScanningComplete => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigateTo<ExchangeIDScanCompleteViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepPrevious = async () => await _state.PreviousAsync();
                    vm.OnStepNext = async _ => await _state.NextAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.Deposit => _nav.NavigateTo<ExchangeDepositViewModel>(vm =>
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
            ExchangeState.Withdrawal => _nav.NavigateTo<ExchangeWithdrawalViewModel>(vm =>
            {
                vm.OnStepNext = async _ => await _state.NextAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Result => _nav.NavigateTo<ExchangeResultViewModel>(vm =>
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
            ExchangeState.Complete => _nav.NavigateTo<ExchangeCompleteViewModel>(vm =>
            {
                vm.OnStepMain = async () => await _state.ExitAsync();
                vm.OnStepError = async ex =>
                {
                    _logging.Error(ex, $"OnStepError, {ex.Message}");
                    await _state.ErrorAsync();
                };
            }),
            ExchangeState.Exit => _nav.SwitchSubShell<MenuSubShellViewModel>(),
            _ => Task.CompletedTask
        };
    }
}
