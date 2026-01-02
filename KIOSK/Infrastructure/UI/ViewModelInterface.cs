namespace KIOSK.ViewModels
{
    // TODO: 뷰모델 공통 인터페이스 재정의 필요
    public interface IStepError
    {
        Action<Exception>? OnStepError { get; set; }
    }

    public interface IStepMain
    {
        Func<Task>? OnStepMain { get; set; }
    }

    public interface IStepNext
    {
        Func<string?, Task>? OnStepNext { get; set; }
    }

    public interface IStepPrevious
    {
        Func<Task>? OnStepPrevious { get; set; }
    }

    public interface INavigable
    {
        Task OnLoadAsync(object? parameter, CancellationToken ct);
        Task OnUnloadAsync();
    }
}
