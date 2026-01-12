using KIOSK.Application.Services;
using KIOSK.Application.StateMachines;
using KIOSK.Infrastructure.Logging;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Presentation.Features.GTF.ViewModels;
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
                GtfState.Language => _nav.NavigateTo<GtfLanguageSelectViewModel>(vm =>
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
                GtfState.IdScanConsent => _nav.NavigateTo<GtfIdScanConsentViewModel>(vm =>
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
                GtfState.IdScanGuide => _nav.NavigateTo<GtfIdScanGuideViewModel>(vm =>
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
                GtfState.IdScanProcess => _nav.NavigateTo<GtfIdScanProcessViewModel>(vm =>
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
                GtfState.RefundMethodSelect => _nav.NavigateTo<GtfRefundMethodSelectViewModel>(vm =>
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
                GtfState.CreditGuide => _nav.NavigateTo<GtfCreditGuideViewModel>(vm =>
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
                GtfState.AlipayGuide => _nav.NavigateTo<GtfAlipayGuideViewModel>(vm =>
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
                GtfState.WeChatGuide => _nav.NavigateTo<GtfWeChatGuideViewModel>(vm =>
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
                GtfState.RefundVoucherRegister => _nav.NavigateTo<GtfRefundVoucherRegisterViewModel>(vm =>
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
                GtfState.Sign => _nav.NavigateTo<GtfRefundSignatureViewModel>(vm =>
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
                GtfState.CreditRegister => _nav.NavigateTo<GtfCreditRegisterViewModel>(vm =>
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
                GtfState.AlipayRegister => _nav.NavigateTo<GtfAlipayRegisterViewModel>(vm =>
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
                GtfState.WeChatRegisterGuide => _nav.NavigateTo<GtfWeChatRegisterGuideViewModel>(vm =>
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
                GtfState.AlipayAccountSelect => _nav.NavigateTo<GtfAlipayAccountSelectViewModel>(vm =>
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
                GtfState.WeChatRegister => _nav.NavigateTo<GtfWeChatRegisterViewModel>(vm =>
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
                GtfState.RefundComplete => _nav.NavigateTo<GtfRefundCompleteViewModel>(vm =>
                {
                    vm.OnStepMain = async () => await _state.ExitAsync();
                    vm.OnStepError = async ex =>
                    {
                        _logging.Error(ex, $"OnStepError, {ex.Message}");
                        await _state.ErrorAsync();
                    };
                }),
                GtfState.Exit => _nav.SwitchSubShell<MenuSubShellViewModel>(),
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
