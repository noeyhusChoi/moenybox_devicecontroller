using System.ComponentModel;

namespace Kiosk.ViewModels.Steps;

public interface IScanIntroStepViewModel : INotifyPropertyChanged
{
    bool CanProceed { get; }
    string PreviewVideoPath { get; }
}
