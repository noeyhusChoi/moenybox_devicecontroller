using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Abstractions;

/// <summary>
/// Provides shared lifecycle wiring for step-based view models so each page
/// only needs to implement feature-specific behavior.
/// </summary>
public abstract class PageViewModelBase : ObservableObject, IStepWorkflow, INavigable
{
    public Func<object?, Task>? OnStepMain { get; set; }
    public Func<object?, Task>? OnStepPrevious { get; set; }
    public Func<object?, Task>? OnStepNext { get; set; }
    public Func<Exception, Task>? OnStepError { get; set; }

    public abstract Task OnLoadAsync(object? parameter, CancellationToken ct);
    public abstract Task OnUnloadAsync();

    protected Task ExecuteStepAsync(Func<object?, Task>? step, object? parameter = null) =>
        ExecuteGuardedAsync(async () =>
        {
            if (step is null)
            {
                return;
            }

            await step(parameter);
        });

    private async Task ExecuteGuardedAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await RaiseStepErrorAsync(ex);
        }
    }

    protected Task RaiseStepErrorAsync(Exception ex) =>
        OnStepError is null ? Task.CompletedTask : OnStepError(ex);
}
