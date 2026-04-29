using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardServiceSelectionStepViewModel : ExchangeStepViewModelBase
{
    public PrepaidCardServiceSelectionStepViewModel(
        IAsyncRelayCommand purchaseAndChargeCommand,
        IAsyncRelayCommand chargeExistingCardCommand)
    {
        PurchaseAndChargeCommand = purchaseAndChargeCommand;
        ChargeExistingCardCommand = chargeExistingCardCommand;
    }

    public IAsyncRelayCommand PurchaseAndChargeCommand { get; }
    public IAsyncRelayCommand ChargeExistingCardCommand { get; }
}
