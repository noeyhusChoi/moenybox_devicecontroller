using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Abstractions
{
   

    public interface IStepWorkflow
    {
        Func<Exception, Task>? OnStepError { get; set; }
        Func<object?, Task>? OnStepMain { get; set; }
        Func<object?, Task>? OnStepNext { get; set; }
        Func<object?, Task>? OnStepPrevious { get; set; }

    }

    public interface IViewLifecycle
    {
        Task OnLoadAsync(object? parameter, CancellationToken ct);
        Task OnUnloadAsync();
    }

    // Backward-compatible alias used across existing navigation code.
    public interface INavigable : IViewLifecycle
    {
    }
}
