using System;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Shared.Abstractions
{
    public interface IStepError
    {
        Action<Exception>? OnStepError { get; set; }
    }

    public interface IStepMain
    {
        Func<object?, Task>? OnStepMain { get; set; }
    }

    public interface IStepNext
    {
        Func<object?, Task>? OnStepNext { get; set; }
    }

    public interface IStepPrevious
    {
        Func<object?, Task>? OnStepPrevious { get; set; }
    }

    public interface IStepLifecycle : IStepMain, IStepNext, IStepPrevious, IStepError
    {
    }

    public interface INavigable
    {
        Task OnLoadAsync(object? parameter, CancellationToken ct);
        Task OnUnloadAsync();
    }
}
