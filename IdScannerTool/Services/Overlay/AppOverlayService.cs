namespace IdScannerTool.Services;

public sealed class AppOverlayService : IAppOverlayService
{
    private readonly object _sync = new();
    private AppOverlaySnapshot _current = new(
        IsVisible: false,
        Title: string.Empty,
        Message: string.Empty,
        ShowIndicator: false,
        IndicatorState: AppOverlayIndicatorState.None,
        ShowConfirmButton: false);

    public event EventHandler<AppOverlaySnapshot>? OverlayChanged;

    public AppOverlaySnapshot Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
    }

    public void ShowProgress(string title, string message)
    {
        Publish(new AppOverlaySnapshot(
            IsVisible: true,
            Title: title,
            Message: message,
            ShowIndicator: true,
            IndicatorState: AppOverlayIndicatorState.Running,
            ShowConfirmButton: false));
    }

    public void UpdateProgressMessage(string message)
    {
        lock (_sync)
        {
            if (!_current.IsVisible)
            {
                return;
            }

            PublishUnsafe(_current with { Message = message });
        }
    }

    public void ShowResult(string title, string message, bool success)
    {
        Publish(new AppOverlaySnapshot(
            IsVisible: true,
            Title: title,
            Message: message,
            ShowIndicator: true,
            IndicatorState: success ? AppOverlayIndicatorState.Succeeded : AppOverlayIndicatorState.Failed,
            ShowConfirmButton: false));
    }

    public void ShowConfirmation(string title, string message)
    {
        Publish(new AppOverlaySnapshot(
            IsVisible: true,
            Title: title,
            Message: message,
            ShowIndicator: false,
            IndicatorState: AppOverlayIndicatorState.None,
            ShowConfirmButton: true));
    }

    public void Hide()
    {
        lock (_sync)
        {
            PublishUnsafe(new AppOverlaySnapshot(
                IsVisible: false,
                Title: string.Empty,
                Message: string.Empty,
                ShowIndicator: false,
                IndicatorState: AppOverlayIndicatorState.None,
                ShowConfirmButton: false));
        }
    }

    private void Publish(AppOverlaySnapshot snapshot)
    {
        lock (_sync)
        {
            PublishUnsafe(snapshot);
        }
    }

    private void PublishUnsafe(AppOverlaySnapshot snapshot)
    {
        _current = snapshot;
        OverlayChanged?.Invoke(this, _current);
    }
}
