using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Abstractions;
using Stateless;

namespace KIOSK.Application.StateMachines;

public abstract class WorkflowStateMachine<TState>
    where TState : struct, Enum
{
    private readonly ILoggingService _logging;
    private readonly StateMachine<TState, StateMachineTrigger> _fsm;
    private readonly Stack<TState> _history = new();
    private readonly SemaphoreSlim _fireLock = new(1, 1);
    
    private readonly TState _exitState;

    protected WorkflowStateMachine(ILoggingService logging, TState startState, TState exitState)
    {
        _logging = logging;
        _exitState = exitState;

        _fsm = new StateMachine<TState, StateMachineTrigger>(startState);

        _fsm.OnTransitionedAsync(async transition =>
        {
            _logging.Info($"{transition.Source} -> {transition.Destination} via {transition.Trigger}");

            if (transition.Trigger == StateMachineTrigger.Next)
            {
                _history.Push(transition.Source);
            }

            if (transition.Trigger == StateMachineTrigger.Previous && _history.Count > 0)
            {
                _history.Pop();
            }

            if (EqualityComparer<TState>.Default.Equals(transition.Destination, _exitState))
            {
                _history.Clear();
            }

            if (StateEntered is { } handler)
            {
                await handler(transition.Destination);
            }
        });
    }

    public event Func<TState, Task>? StateEntered;

    protected StateMachine<TState, StateMachineTrigger> StateMachine => _fsm;

    protected void ConfigureStateMachine(Action<StateMachine<TState, StateMachineTrigger>> configure) => 
        configure(_fsm);

    protected TState GetHistoryOrExit() => _history.Count > 0 ? _history.Peek() : _exitState;

    protected Task FireTriggerAsync(StateMachineTrigger trigger) =>
        FireAsync(trigger, () => _fsm.FireAsync(trigger));

    protected Task FireParameterizedTriggerAsync<TArg>(StateMachine<TState, StateMachineTrigger>.TriggerWithParameters<TArg> trigger, TArg argument, StateMachineTrigger triggerId) =>
        FireAsync(triggerId, () => _fsm.FireAsync(trigger, argument));

    private async Task FireAsync(StateMachineTrigger trigger, Func<Task> fireAction)
    {
        await _fireLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await fireAction().ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logging.Error(ex, $"invalid transition ({trigger}): {ex.Message}");
        }
        catch (Exception ex)
        {
            _logging.Error(ex, $"fire error ({trigger}): {ex.Message}");
        }
        finally
        {
            _fireLock.Release();
        }
    }
    public Task NextAsync() => FireTriggerAsync(StateMachineTrigger.Next);
    
    public virtual Task NextAsync(string? parameter) => NextAsync();

    public Task PreviousAsync() => FireTriggerAsync(StateMachineTrigger.Previous);

    public Task ExitAsync() => FireTriggerAsync(StateMachineTrigger.Exit);

    public Task ErrorAsync() => FireTriggerAsync(StateMachineTrigger.Error);

    public Task StartAsync()
    {
        if (StateEntered is { } handler)
        {
            return handler(_fsm.State);
        }

        return Task.CompletedTask;
    }

    public TState CurrentState => _fsm.State;
}
