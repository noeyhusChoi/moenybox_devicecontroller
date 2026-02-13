using KIOSK.Application.StateMachines;
using KIOSK.Application.Abstractions;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.Menu.Layout.ViewModels;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels;
using System.Windows.Threading;
using System.Threading.Tasks;
using KIOSK.Presentation.Abstractions;

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
                BindDefaultHandlers(vm)),
            ExchangeState.Currency => _nav.NavigatePage<ExchangeCurrencyViewModel>(vm =>
                BindDefaultHandlers(vm)),
            ExchangeState.Terms => _nav.NavigatePage<ExchangeIDScanConsentViewModel>(vm =>
                BindDefaultHandlers(vm)),
            ExchangeState.IDScan => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigatePage<ExchangeIDScanGuideViewModel>(vm =>
                    BindDefaultHandlers(vm)), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.IDScanning => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigatePage<ExchangeIDScanProcessViewModel>(vm =>
                    BindDefaultHandlers(vm)), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.IDScanningComplete => System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                await _nav.NavigatePage<ExchangeIDScanCompleteViewModel>(vm =>
                    BindDefaultHandlers(vm)), DispatcherPriority.ApplicationIdle).Task,
            ExchangeState.Deposit => _nav.NavigatePage<ExchangeDepositViewModel>(vm =>
                BindDefaultHandlers(vm)),
            ExchangeState.Withdrawal => _nav.NavigatePage<ExchangeWithdrawalViewModel>(vm =>
                BindDefaultHandlers(vm, enableMain: false, enablePrevious: false)),
            ExchangeState.Result => _nav.NavigatePage<ExchangeResultViewModel>(vm =>
                BindDefaultHandlers(vm)),
            ExchangeState.Complete => _nav.NavigatePage<ExchangeCompleteViewModel>(vm =>
                BindDefaultHandlers(vm, enablePrevious: false, enableNext: false)),
            ExchangeState.Exit => _nav.NavigateLayout<MenuLayoutViewModel>(),
            _ => Task.CompletedTask
        };

        private void BindDefaultHandlers(
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
