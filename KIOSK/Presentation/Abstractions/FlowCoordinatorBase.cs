using System;
using System.Threading.Tasks;
using KIOSK.Application.Abstractions;
using KIOSK.Application.StateMachines;
using KIOSK.Presentation.Navigation.Services;

namespace KIOSK.Presentation.Abstractions;

public abstract class FlowCoordinatorBase<TState>
    where TState : struct, Enum
{
    private readonly INavigationService _navigation;
    protected readonly ILoggingService Logging;
    protected readonly WorkflowStateMachine<TState> StateMachine;

    protected FlowCoordinatorBase(
        INavigationService navigation,
        ILoggingService logging,
        WorkflowStateMachine<TState> stateMachine)
    {
        _navigation = navigation;
        Logging = logging;
        StateMachine = stateMachine;
        StateMachine.StateEntered += OnStateEnteredAsync;
    }

    protected abstract Task HandleStateAsync(TState state);

    protected Task NavigatePage<TViewModel>(Action<TViewModel> configure)
        where TViewModel : class => _navigation.NavigatePage(configure);

    protected Task NavigateLayout<TLayoutViewModel>()
        where TLayoutViewModel : class, ILayout => _navigation.NavigateLayout<TLayoutViewModel>();

    protected Task NavigatePageOnDispatcher<TViewModel>(Func<Task> navigationTask) => navigationTask();

    protected void BindDefaultHandlers(
        PageViewModelBase viewModel,
        bool enableMain = true,
        bool enablePrevious = true,
        bool enableNext = true,
        Func<object?, Task>? nextOverride = null)
    {
        viewModel.OnStepMain = enableMain ? async _ => await StateMachine.ExitAsync() : null;
        viewModel.OnStepPrevious = enablePrevious ? async _ => await StateMachine.PreviousAsync() : null;
        viewModel.OnStepNext = enableNext
            ? nextOverride ?? (async _ => await StateMachine.NextAsync())
            : null;
        viewModel.OnStepError = async ex =>
        {
            Logging.Error(ex, $"OnStepError, {ex.Message}");
            await StateMachine.ErrorAsync();
        };
    }

    private async Task OnStateEnteredAsync(TState state) => await HandleStateAsync(state);

    public Task StartAsync() => StateMachine.StartAsync();
}
