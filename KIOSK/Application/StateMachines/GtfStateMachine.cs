using System.Threading.Tasks;
using KIOSK.Application.Abstractions;
using Stateless;

namespace KIOSK.Application.StateMachines;

public enum GtfState
{
    Start,
    Language,
    IdScanConsent,
    IdScanGuide,
    IdScanProcess,
    IdScanComplete,
    RefundMethodSelect,
    RefundMethodGuide,
    AlipayGuide,
    CreditGuide,
    WeChatGuide,
    AlipayRegister,
    CreditRegister,
    WeChatRegister,
    AlipayAccountSelect,
    WeChatRegisterGuide,
    Info,
    RegisterQR,
    Sign,
    RefundVoucherRegister,
    RefundComplete,
    Exit,
    Error
}

public sealed class GtfStateMachine : WorkflowStateMachine<GtfState>
{
    private readonly StateMachine<GtfState, StateMachineTrigger>.TriggerWithParameters<string> _nextTrigger;

    public GtfStateMachine(ILoggingService logging)
        : base(logging, GtfState.Language, GtfState.Exit)
    {
        _nextTrigger = StateMachine.SetTriggerParameters<string>(StateMachineTrigger.Next);
        ConfigureStates();
    }

    private void ConfigureStates()
    {
        ConfigureStateMachine(fsm =>
        {
            fsm.Configure(GtfState.Language)
                .Permit(StateMachineTrigger.Next, GtfState.IdScanConsent)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.IdScanConsent)
                .Permit(StateMachineTrigger.Next, GtfState.IdScanGuide)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.IdScanGuide)
                .Permit(StateMachineTrigger.Next, GtfState.IdScanProcess)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.IdScanProcess)
                .Permit(StateMachineTrigger.Next, GtfState.RefundMethodSelect)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.RefundMethodSelect)
                .PermitDynamic(_nextTrigger, key => key?.ToLowerInvariant() switch
                {
                    "credit" => GtfState.CreditGuide,
                    "alipay" => GtfState.AlipayGuide,
                    "wechat" => GtfState.WeChatGuide,
                    _ => GtfState.Error
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.CreditGuide)
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.AlipayGuide)
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.WeChatGuide)
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.RefundVoucherRegister)
                .PermitDynamic(_nextTrigger, key => key?.ToLowerInvariant() switch
                {
                    "sign" => GtfState.Sign,
                    "credit" => GtfState.CreditRegister,
                    "alipay" => GtfState.AlipayRegister,
                    "wechat" => GtfState.WeChatRegisterGuide,
                    _ => GtfState.Error
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.Sign)
                .PermitDynamic(_nextTrigger, key => key?.ToLowerInvariant() switch
                {
                    "credit" => GtfState.CreditRegister,
                    "alipay" => GtfState.AlipayRegister,
                    "wechat" => GtfState.WeChatRegisterGuide,
                    _ => GtfState.Error
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.CreditRegister)
                .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.AlipayRegister)
                .Permit(StateMachineTrigger.Next, GtfState.AlipayAccountSelect)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.WeChatRegisterGuide)
                .Permit(StateMachineTrigger.Next, GtfState.WeChatRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.AlipayAccountSelect)
                .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.WeChatRegister)
                .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

            fsm.Configure(GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error);

            fsm.Configure(GtfState.Exit);

            fsm.Configure(GtfState.Error)
                .OnEntryAsync(async () => await PreviousAsync())
                .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);
        });
    }

    // 파라미터로 분기 처리 로직
    public override async Task NextAsync(string? parameter)
    {
        var key = parameter ?? string.Empty;
        await FireParameterizedTriggerAsync(_nextTrigger, key, StateMachineTrigger.Next).ConfigureAwait(false);
    }
}
