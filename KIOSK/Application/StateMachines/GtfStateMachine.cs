using KIOSK.FSM;
using KIOSK.Infrastructure.Logging;
using Stateless;

namespace KIOSK.Application.StateMachines
{
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

    public sealed class GtfStateMachine
    {
        private readonly ILoggingService _logging;
        private readonly StateMachine<GtfState, StateMachineTrigger> _fsm;
        private readonly Stack<GtfState> _history = new();
        private readonly SemaphoreSlim _fireLock = new(1, 1);
        private readonly StateMachine<GtfState, StateMachineTrigger>.TriggerWithParameters<string> _nextTrigger;

        public event Func<GtfState, Task>? StateEntered;

        public GtfStateMachine(ILoggingService logging)
        {
            _logging = logging;
            _fsm = new StateMachine<GtfState, StateMachineTrigger>(GtfState.Start);
            _nextTrigger = _fsm.SetTriggerParameters<string>(StateMachineTrigger.Next);

            _fsm.OnTransitionedAsync(async transition =>
            {
                _logging.Info($"{transition.Source} -> {transition.Destination} via {transition.Trigger}");

                if (transition.Trigger.Equals(StateMachineTrigger.Previous) && _history.Count > 0)
                {
                    _history.Pop();
                }

                if (transition.Destination == GtfState.Exit)
                {
                    _history.Clear();
                }

                var handler = StateEntered;
                if (handler != null)
                {
                    await handler(transition.Destination);
                }
            });

            ConfigureStates();
        }

        private void ConfigureStates()
        {
            _fsm.Configure(GtfState.Start)
                .OnEntryAsync(async () => await NextAsync())
                .Permit(StateMachineTrigger.Next, GtfState.Language);

            _fsm.Configure(GtfState.Language)
                .Permit(StateMachineTrigger.Next, GtfState.IdScanConsent)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.IdScanConsent)
                .Permit(StateMachineTrigger.Next, GtfState.IdScanGuide)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.IdScanGuide)
                .Permit(StateMachineTrigger.Next, GtfState.IdScanProcess)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.IdScanProcess)
                .Permit(StateMachineTrigger.Next, GtfState.RefundMethodSelect)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.RefundMethodSelect)
                .PermitDynamic(_nextTrigger, method =>
                {
                    var methodLower = method?.ToLowerInvariant();
                    return methodLower switch
                    {
                        "credit" => GtfState.CreditGuide,
                        "alipay" => GtfState.AlipayGuide,
                        "wechat" => GtfState.WeChatGuide,
                        _ => GtfState.Error
                    };
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.CreditGuide)
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.AlipayGuide)
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.WeChatGuide)
                .Permit(StateMachineTrigger.Next, GtfState.RefundVoucherRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.RefundVoucherRegister)
                .PermitDynamic(_nextTrigger, method =>
                {
                    var methodLower = method?.ToLowerInvariant();
                    return methodLower switch
                    {
                        "sign" => GtfState.Sign,
                        "credit" => GtfState.CreditRegister,
                        "alipay" => GtfState.AlipayRegister,
                        "wechat" => GtfState.WeChatRegisterGuide,
                        _ => GtfState.Error
                    };
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.Sign)
                .PermitDynamic(_nextTrigger, method =>
                {
                    var methodLower = method?.ToLowerInvariant();
                    return methodLower switch
                    {
                        "credit" => GtfState.CreditRegister,
                        "alipay" => GtfState.AlipayRegister,
                        "wechat" => GtfState.WeChatRegisterGuide,
                        _ => GtfState.Error
                    };
                })
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.CreditRegister)
                .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.AlipayRegister)
                .Permit(StateMachineTrigger.Next, GtfState.AlipayAccountSelect)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.WeChatRegisterGuide)
                .Permit(StateMachineTrigger.Next, GtfState.WeChatRegister)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.AlipayAccountSelect)
                .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.WeChatRegister)
                .Permit(StateMachineTrigger.Next, GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);

            _fsm.Configure(GtfState.RefundComplete)
                .Permit(StateMachineTrigger.Exit, GtfState.Exit)
                .Permit(StateMachineTrigger.Error, GtfState.Error);

            _fsm.Configure(GtfState.Exit);

            _fsm.Configure(GtfState.Error)
                .OnEntryAsync(async () => await PreviousAsync())
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : GtfState.Exit);
        }

        private async Task FireAsyncSafe(StateMachineTrigger trigger)
        {
            await _fireLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _fsm.FireAsync(trigger).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                _logging.Error(ex, $"invalid transition: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logging.Error(ex, $"fire error: {ex.Message}");
            }
            finally
            {
                _fireLock.Release();
            }
        }

        public async Task NextAsync()
        {
            if (_fsm.State != GtfState.Start)
            {
                _history.Push(_fsm.State);
            }

            await FireAsyncSafe(StateMachineTrigger.Next);
        }

        public async Task NextAsync(string? key)
        {
            if (_fsm.State != GtfState.Start)
            {
                _history.Push(_fsm.State);
            }

            await _fireLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _fsm.FireAsync(StateMachineTrigger.Next, key ?? string.Empty).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                _logging.Error(ex, $"invalid transition: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logging.Error(ex, $"fire error: {ex.Message}");
            }
            finally
            {
                _fireLock.Release();
            }
        }

        public Task PreviousAsync() => FireAsyncSafe(StateMachineTrigger.Previous);
        public Task ExitAsync() => FireAsyncSafe(StateMachineTrigger.Exit);
        public Task ErrorAsync() => FireAsyncSafe(StateMachineTrigger.Error);

        public Task StartAsync() => NextAsync();
        public Task FireNextAsync() => NextAsync();
        public Task FirePreviousAsync() => PreviousAsync();
        public Task FireMainAsync() => ExitAsync();
        public Task FireErrorAsync() => ErrorAsync();

        public GtfState CurrentState => _fsm.State;
    }
}
