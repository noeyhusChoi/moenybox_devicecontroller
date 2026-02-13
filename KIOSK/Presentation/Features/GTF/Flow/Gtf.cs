using KIOSK.Application.StateMachines;
using KIOSK.Application.Abstractions;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Services;
using KIOSK.Presentation.Features.GTF.Pages.ViewModels;
using KIOSK.Presentation.Features.Menu.Layout.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Layout.ViewModels;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Flow
{
    public sealed class Gtf : FlowCoordinatorBase<GtfState>
    {
        private readonly IInactivityService _idle;

        public Gtf(
            INavigationService nav,
            ILoggingService logging,
            IInactivityService idle,
            GtfStateMachine state)
            : base(nav, logging, state)
        {
            _idle = idle;
        }

        protected override Task HandleStateAsync(GtfState state)
        {
            HandleIdle(state);

            return state switch
            {
                GtfState.Language => NavigatePage<GtfLanguageSelectViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.IdScanConsent => NavigatePage<GtfIdScanConsentViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.IdScanGuide => NavigatePage<GtfIdScanGuideViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.IdScanProcess => NavigatePage<GtfIdScanProcessViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.RefundMethodSelect => NavigatePage<GtfRefundMethodSelectViewModel>(vm =>
                    BindDefaultHandlers(vm, nextOverride: async param => await StateMachine.NextAsync(param as string))),
                GtfState.CreditGuide => NavigatePage<GtfCreditGuideViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.AlipayGuide => NavigatePage<GtfAlipayGuideViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.WeChatGuide => NavigatePage<GtfWeChatGuideViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.RefundVoucherRegister => NavigatePage<GtfRefundVoucherRegisterViewModel>(vm =>
                    BindDefaultHandlers(vm, nextOverride: async param => await StateMachine.NextAsync(param as string))),
                GtfState.Sign => NavigatePage<GtfRefundSignatureViewModel>(vm =>
                    BindDefaultHandlers(vm, nextOverride: async param => await StateMachine.NextAsync(param as string))),
                GtfState.CreditRegister => NavigatePage<GtfCreditRegisterViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.AlipayRegister => NavigatePage<GtfAlipayRegisterViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.WeChatRegisterGuide => NavigatePage<GtfWeChatRegisterGuideViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.AlipayAccountSelect => NavigatePage<GtfAlipayAccountSelectViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.WeChatRegister => NavigatePage<GtfWeChatRegisterViewModel>(vm =>
                    BindDefaultHandlers(vm)),
                GtfState.RefundComplete => NavigatePage<GtfRefundCompleteViewModel>(vm =>
                    BindDefaultHandlers(vm, enablePrevious: false, enableNext: false)),
                GtfState.Exit => NavigateLayout<MenuV2LayoutViewModel>(),
                _ => Task.CompletedTask
            };
        }

        private void HandleIdle(GtfState state)
        {
            if (state == GtfState.Language)
            {
                _idle.Start(TimeSpan.FromMinutes(1), async () => await StateMachine.ExitAsync());
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
