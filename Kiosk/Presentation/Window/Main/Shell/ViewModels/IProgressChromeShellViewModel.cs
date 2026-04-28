namespace Kiosk.ViewModels;

public interface IProgressChromeShellViewModel
{
    string TimerText { get; }
    bool ShowStepHeader { get; }
    bool CollapseShellChrome { get; }
}
