namespace IdScannerTool.Services;

public interface IAppOverlayService
{
    event EventHandler<AppOverlaySnapshot>? OverlayChanged;

    AppOverlaySnapshot Current { get; }

    void ShowProgress(string title, string message);
    void UpdateProgressMessage(string message);
    void ShowResult(string title, string message, bool success);
    void ShowConfirmation(string title, string message);
    void Hide();
}

public enum AppOverlayIndicatorState
{
    None = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public sealed record AppOverlaySnapshot(
    bool IsVisible,
    string Title,
    string Message,
    bool ShowIndicator,
    AppOverlayIndicatorState IndicatorState,
    bool ShowConfirmButton);
