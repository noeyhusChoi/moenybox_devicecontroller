using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardChargeMethodSelectionStepViewModel : ExchangeStepViewModelBase
{
    public PrepaidCardChargeMethodSelectionStepViewModel(
        IAsyncRelayCommand chargeBothWalletsCommand,
        IAsyncRelayCommand chargeTrafficWalletOnlyCommand)
    {
        ChargeBothWalletsCommand = chargeBothWalletsCommand;
        ChargeTrafficWalletOnlyCommand = chargeTrafficWalletOnlyCommand;
    }

    public IAsyncRelayCommand ChargeBothWalletsCommand { get; }
    public IAsyncRelayCommand ChargeTrafficWalletOnlyCommand { get; }
}
