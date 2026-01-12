using KIOSK.FSM;
using KIOSK.Infrastructure.Logging;
using Stateless;

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

    public sealed class ExchangeSellStateMachine
    {
        private readonly ILoggingService _logging;
        private readonly StateMachine<ExchangeState, StateMachineTrigger> _fsm;
        private readonly Stack<ExchangeState> _history = new();
        private readonly SemaphoreSlim _fireLock = new(1, 1);

        public event Func<ExchangeState, Task>? StateEntered;

        public ExchangeSellStateMachine(ILoggingService logging)
        {
            _logging = logging;
            _fsm = new StateMachine<ExchangeState, StateMachineTrigger>(ExchangeState.Start);

            _fsm.OnTransitionedAsync(async transition =>
            {
                _logging.Info($"{transition.Source} -> {transition.Destination} via {transition.Trigger}");

                if (transition.Trigger.Equals(StateMachineTrigger.Previous) && _history.Count > 0)
                {
                    _history.Pop();
                }

                if (transition.Destination == ExchangeState.Exit)
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
            _fsm.Configure(ExchangeState.Start)
                .OnEntryAsync(async () => await NextAsync())
                .Permit(StateMachineTrigger.Next, ExchangeState.Language);

            _fsm.Configure(ExchangeState.Language)
                .Permit(StateMachineTrigger.Next, ExchangeState.Currency)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.Currency)
                .Permit(StateMachineTrigger.Next, ExchangeState.Terms)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.Terms)
                .Permit(StateMachineTrigger.Next, ExchangeState.IDScan)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.IDScan)
                .Permit(StateMachineTrigger.Next, ExchangeState.IDScanning)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.IDScanning)
                .Permit(StateMachineTrigger.Next, ExchangeState.IDScanningComplete)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.IDScanningComplete)
                .Permit(StateMachineTrigger.Next, ExchangeState.Deposit)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.Deposit)
                .Permit(StateMachineTrigger.Next, ExchangeState.Withdrawal)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.Withdrawal)
                .Permit(StateMachineTrigger.Next, ExchangeState.Result)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.Result)
                .Permit(StateMachineTrigger.Next, ExchangeState.Complete)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeState.Exit);

            _fsm.Configure(ExchangeState.Complete)
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeState.Error);

            _fsm.Configure(ExchangeState.Error)
                .OnEntryAsync(async () => await ExitAsync())
                .Permit(StateMachineTrigger.Exit, ExchangeState.Exit);
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
            if (_fsm.State != ExchangeState.Start)
            {
                _history.Push(_fsm.State);
            }

            await FireAsyncSafe(StateMachineTrigger.Next);
        }

        public async Task NextAsync(string? key)
        {
            if (_fsm.State != ExchangeState.Start)
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

        public ExchangeState CurrentState => _fsm.State;
    }
}
