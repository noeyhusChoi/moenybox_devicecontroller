using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kiosk.ViewModels.Steps;

public partial class ConsentStepViewModel : ExchangeStepViewModelBase, ITermsAgreementStepViewModel
{
    public ConsentStepViewModel(IAsyncRelayCommand viewTermsCommand)
    {
        ViewTermsCommand = viewTermsCommand;
    }

    [ObservableProperty]
    private bool isAgreed;

    partial void OnIsAgreedChanged(bool value)
        => IsPrimaryEnabled = value;

    public IAsyncRelayCommand ViewTermsCommand { get; }
}
