using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardWalletSelectionStepViewModel : ExchangeStepViewModelBase
{
    public PrepaidCardWalletSelectionStepViewModel(
        IAsyncRelayCommand chargePrepaidWalletCommand,
        IAsyncRelayCommand chargeTrafficWalletCommand)
    {
        ChargePrepaidWalletCommand = chargePrepaidWalletCommand;
        ChargeTrafficWalletCommand = chargeTrafficWalletCommand;
    }

    public IAsyncRelayCommand ChargePrepaidWalletCommand { get; }

    public IAsyncRelayCommand ChargeTrafficWalletCommand { get; }
}
