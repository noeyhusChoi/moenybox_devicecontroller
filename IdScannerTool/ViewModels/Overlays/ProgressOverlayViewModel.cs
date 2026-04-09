using IdScannerTool.Services;

namespace IdScannerTool.ViewModels.Overlays;

public sealed class ProgressOverlayViewModel
{
    public ProgressOverlayViewModel(string title, string message, AppOverlayIndicatorState indicatorState)
    {
        Title = title;
        Message = message;
        IndicatorState = indicatorState;
    }

    public string Title { get; }
    public string Message { get; }
    public AppOverlayIndicatorState IndicatorState { get; }

    public bool IsSpinning => IndicatorState == AppOverlayIndicatorState.Running;

    public string IndicatorText => IndicatorState switch
    {
        AppOverlayIndicatorState.Succeeded => "✔",
        AppOverlayIndicatorState.Failed => "✕",
        _ => "○"
    };
}
