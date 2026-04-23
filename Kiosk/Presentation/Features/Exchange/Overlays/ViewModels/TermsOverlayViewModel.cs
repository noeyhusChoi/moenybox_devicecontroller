using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kiosk.ViewModels.Overlays;

public partial class TermsOverlayViewModel : ObservableObject
{
    public TermsOverlayViewModel(IRelayCommand closeCommand)
    {
        CloseCommand = closeCommand;
    }

    public IRelayCommand CloseCommand { get; }
}
