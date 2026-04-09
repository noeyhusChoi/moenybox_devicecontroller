using CommunityToolkit.Mvvm.Input;

namespace IdScannerTool.ViewModels.Overlays;

public sealed class ConfirmOverlayViewModel
{
    public ConfirmOverlayViewModel(string title, string message, Action onConfirm)
    {
        Title = title;
        Message = message;
        ConfirmCommand = new RelayCommand(onConfirm);
    }

    public string Title { get; }
    public string Message { get; }
    public IRelayCommand ConfirmCommand { get; }
}
