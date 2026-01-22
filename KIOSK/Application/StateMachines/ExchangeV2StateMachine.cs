using KIOSK.FSM;
using KIOSK.Application.Abstractions;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Application.StateMachines
{

    public enum ExchangeV2State
    {
        Start,
        Language,
        Type,
        Method,
        Currency,
        IdScanConsent,
        Terms,
        Error,
        Exit
    }

    public sealed class ExchangeV2StateMachine
    {
        private readonly ILoggingService _logging;
        private readonly StateMachine<ExchangeV2State, StateMachineTrigger> _fsm;
        private readonly Stack<ExchangeV2State> _history = new();
        private readonly SemaphoreSlim _fireLock = new(1, 1);

        public event Func<ExchangeV2State, Task>? StateEntered;

        public ExchangeV2StateMachine(ILoggingService logging)
        {
            _logging = logging;
            _fsm = new StateMachine<ExchangeV2State, StateMachineTrigger>(ExchangeV2State.Start);

            _fsm.OnTransitionedAsync(async transition =>
            {
                _logging.Info($"[Navigation] From={transition.Source} To={transition.Destination} Trigger={transition.Trigger}");

                if (transition.Trigger.Equals(StateMachineTrigger.Previous) && _history.Count > 0)
                {
                    _history.Pop();
                }

                if (transition.Destination == ExchangeV2State.Exit)
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
            _fsm.Configure(ExchangeV2State.Start)
                .OnEntryAsync(async () => await NextAsync())
                .Permit(StateMachineTrigger.Next, ExchangeV2State.Language);

            _fsm.Configure(ExchangeV2State.Language)
                .Permit(StateMachineTrigger.Next, ExchangeV2State.Type)
                .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeV2State.Exit);


            _fsm.Configure(ExchangeV2State.Type)
                .Permit(StateMachineTrigger.Next, ExchangeV2State.Method)
                .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeV2State.Exit);

            _fsm.Configure(ExchangeV2State.Method)
                .Permit(StateMachineTrigger.Next, ExchangeV2State.Currency)
                .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeV2State.Exit);


            _fsm.Configure(ExchangeV2State.Currency)
                .Permit(StateMachineTrigger.Next, ExchangeV2State.IdScanConsent)
                .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeV2State.Exit);

            _fsm.Configure(ExchangeV2State.IdScanConsent)
                .Permit(StateMachineTrigger.Next, ExchangeV2State.Exit)
                .Permit(StateMachineTrigger.Exit, ExchangeV2State.Exit)
                .Permit(StateMachineTrigger.Error, ExchangeV2State.Error)
                .PermitDynamic(StateMachineTrigger.Previous, () => _history.Count > 0 ? _history.Peek() : ExchangeV2State.Exit);
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
            if (_fsm.State != ExchangeV2State.Start)
            {
                _history.Push(_fsm.State);
            }

            await FireAsyncSafe(StateMachineTrigger.Next);
        }

        public async Task NextAsync(string? key)
        {
            if (_fsm.State != ExchangeV2State.Start)
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

        public ExchangeV2State CurrentState => _fsm.State;
    }
}

