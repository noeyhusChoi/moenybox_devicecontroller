namespace Kiosk.ViewModels.PrepaidCard;

public sealed class PrepaidCardBaseCashAmountEntryStepViewModel : PrepaidCardAmountEntryStepViewModel
{
    public PrepaidCardBaseCashAmountEntryStepViewModel(
        Func<PrepaidCardWalletKind, Task> showChargeOverlay,
        int availableChargeAmount,
        PrepaidCardServiceKind? serviceKind)
        : base(showChargeOverlay, availableChargeAmount, serviceKind)
    {
    }
}
