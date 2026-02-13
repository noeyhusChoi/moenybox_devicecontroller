using KIOSK.Application.Abstractions;
using KIOSK.Application.StateMachines;

namespace KIOSK.Application.Features.ExchangeV2.StateMachine
{

    public enum ExchangeV2State
    {
        Start,
        Language,
        Type,
        Method,
        Currency,
        IdScanConsent,
        IdScanProcess,
        IdScanComplete,
        Error,
        Exit
    }

    public sealed class ExchangeV2StateMachine : WorkflowStateMachine<ExchangeV2State>
    {
        public ExchangeV2StateMachine(ILoggingService logging)
            : base(logging, ExchangeV2State.Language, ExchangeV2State.Exit)
        {
            ConfigureStates();
        }

        private void ConfigureStates()
        {
            ConfigureStateMachine(fsm =>
            {
                fsm.Configure(ExchangeV2State.Language)
                    .Permit(StateMachineTrigger.Next, ExchangeV2State.Type)
                    .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeV2State.Type)
                    .Permit(StateMachineTrigger.Next, ExchangeV2State.Method)
                    .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeV2State.Method)
                    .Permit(StateMachineTrigger.Next, ExchangeV2State.Currency)
                    .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeV2State.Currency)
                    .Permit(StateMachineTrigger.Next, ExchangeV2State.IdScanConsent)
                    .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeV2State.IdScanConsent)
                    .Permit(StateMachineTrigger.Next, ExchangeV2State.IdScanProcess)
                    .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeV2State.IdScanProcess)
                    .Permit(StateMachineTrigger.Next, ExchangeV2State.IdScanComplete)
                    .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeV2State.IdScanComplete)
                    .Permit(StateMachineTrigger.Next, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);
            });
        }
    }
}

