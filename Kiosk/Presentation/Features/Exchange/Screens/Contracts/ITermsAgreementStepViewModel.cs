using System.ComponentModel;

namespace Kiosk.ViewModels.Steps;

public interface ITermsAgreementStepViewModel : INotifyPropertyChanged
{
    bool IsAgreed { get; }
}
