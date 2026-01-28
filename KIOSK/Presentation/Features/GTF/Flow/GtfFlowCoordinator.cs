using KIOSK.Application.StateMachines;
using KIOSK.Application.Abstractions;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Services;
using KIOSK.Presentation.Features.GTF.Pages.ViewModels;
using KIOSK.Presentation.Features.Menu.Shell.ViewModels;

namespace KIOSK.Presentation.Features.GTF.Flow
{
    public sealed class GtfFlowCoordinator
    {
        private readonly INavigationService _nav;
        private readonly ILoggingService _logging;
        private readonly IInactivityService _idle;
        private readonly GtfStateMachine _state;

        public GtfFlowCoordinator(
            INavigationService nav,
            ILoggingService logging,
            IInactivityService idle,
            GtfStateMachine state)
        {
            _nav = nav;
            _logging = logging;
            _idle = idle;
            _state = state;

            _state.StateEntered += OnStateEnteredAsync;
        }

        public Task StartAsync() => _state.StartAsync();

        private Task OnStateEnteredAsync(GtfState state)
        {
            HandleIdle(state);

            return state switch
            {
                GtfState.Language => _nav.NavigatePage<GtfLanguageSelectViewModel>(vm =>
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
                GtfState.IdScanConsent => _nav.NavigatePage<GtfIdScanConsentViewModel>(vm =>
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
                GtfState.IdScanGuide => _nav.NavigatePage<GtfIdScanGuideViewModel>(vm =>
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
                GtfState.IdScanProcess => _nav.NavigatePage<GtfIdScanProcessViewModel>(vm =>
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
                GtfState.RefundMethodSelect => _nav.NavigatePage<GtfRefundMethodSelectViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepPrevious = async () => await _state.PreviousAsync();
                    vm.OnStepNext = async param => await _state.NextAsync(param);
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }),
                GtfState.CreditGuide => _nav.NavigatePage<GtfCreditGuideViewModel>(vm =>
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
                GtfState.AlipayGuide => _nav.NavigatePage<GtfAlipayGuideViewModel>(vm =>
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
                GtfState.WeChatGuide => _nav.NavigatePage<GtfWeChatGuideViewModel>(vm =>
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
                GtfState.RefundVoucherRegister => _nav.NavigatePage<GtfRefundVoucherRegisterViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepPrevious = async () => await _state.PreviousAsync();
                    vm.OnStepNext = async param => await _state.NextAsync(param);
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }),
                GtfState.Sign => _nav.NavigatePage<GtfRefundSignatureViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepPrevious = async () => await _state.PreviousAsync();
                    vm.OnStepNext = async param => await _state.NextAsync(param);
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }),
                GtfState.CreditRegister => _nav.NavigatePage<GtfCreditRegisterViewModel>(vm =>
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
                GtfState.AlipayRegister => _nav.NavigatePage<GtfAlipayRegisterViewModel>(vm =>
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
                GtfState.WeChatRegisterGuide => _nav.NavigatePage<GtfWeChatRegisterGuideViewModel>(vm =>
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
                GtfState.AlipayAccountSelect => _nav.NavigatePage<GtfAlipayAccountSelectViewModel>(vm =>
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
                GtfState.WeChatRegister => _nav.NavigatePage<GtfWeChatRegisterViewModel>(vm =>
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
                GtfState.RefundComplete => _nav.NavigatePage<GtfRefundCompleteViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }),
                GtfState.Exit => _nav.NavigateLayout<MenuShellViewModel>(),
                _ => Task.CompletedTask
            };
        }

        private void HandleIdle(GtfState state)
        {
            if (state == GtfState.Language)
            {
                _idle.Start(TimeSpan.FromMinutes(1), async () => await _state.ExitAsync());
                return;
            }

            if (state == GtfState.Exit)
            {
                _idle.Stop();
                return;
            }

            _idle.Reset();
        }
    }
}
