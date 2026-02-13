using KIOSK.Application.Abstractions;

namespace KIOSK.Application.StateMachines
{
    public enum ExchangeState
    {
        Start,
        Language,
        Currency,
        Terms,
        IDScan,
        IDScanning,
        IDScanningComplete,
        Deposit,
        ApiRequest,
        Withdrawal,
        Result,
        Complete,
        Error,
        Exit
    }

    public sealed class ExchangeSellStateMachine : WorkflowStateMachine<ExchangeState>
    {
        public ExchangeSellStateMachine(ILoggingService logging) : base(logging, ExchangeState.Language, ExchangeState.Exit)
        {
            ConfigureStates();
        }

        private void ConfigureStates()
        {
            ConfigureStateMachine(fsm =>
            {
                fsm.Configure(ExchangeState.Start)
                    .OnEntryAsync(async () => await NextAsync())
                    .Permit(StateMachineTrigger.Next, ExchangeState.Language);

                fsm.Configure(ExchangeState.Language)
                    .Permit(StateMachineTrigger.Next, ExchangeState.Currency)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.Currency)
                    .Permit(StateMachineTrigger.Next, ExchangeState.Terms)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.Terms)
                    .Permit(StateMachineTrigger.Next, ExchangeState.IDScan)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.IDScan)
                    .Permit(StateMachineTrigger.Next, ExchangeState.IDScanning)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.IDScanning)
                    .Permit(StateMachineTrigger.Next, ExchangeState.IDScanningComplete)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.IDScanningComplete)
                    .Permit(StateMachineTrigger.Next, ExchangeState.Deposit)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.Deposit)
                    .Permit(StateMachineTrigger.Next, ExchangeState.Withdrawal)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.Withdrawal)
                    .Permit(StateMachineTrigger.Next, ExchangeState.Result)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.Result)
                    .Permit(StateMachineTrigger.Next, ExchangeState.Complete)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                    .PermitDynamic(StateMachineTrigger.Previous, GetHistoryOrExit);

                fsm.Configure(ExchangeState.Complete)
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                    .Permit(StateMachineTrigger.Error, ExchangeState.Error);

                fsm.Configure(ExchangeState.Error)
                    .OnEntryAsync(async () => await ExitAsync())
                    .Permit(StateMachineTrigger.Exit, ExchangeState.Exit);
            });
        }
    }
}
