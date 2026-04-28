using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public partial class PrepaidCardAmountEntryStepViewModel : ExchangeStepViewModelBase
{
    private const int WalletMaxAmount = 500_000;
    private const string CurrencyCode = "KRW";

    private readonly Func<PrepaidCardWalletKind, Task> _showChargeOverlay;
    private readonly int _availableChargeAmount;

    [ObservableProperty]
    private int prepaidWalletAmount;

    [ObservableProperty]
    private int trafficWalletAmount;

    public PrepaidCardAmountEntryStepViewModel(
        Func<PrepaidCardWalletKind, Task> showChargeOverlay,
        int availableChargeAmount,
        PrepaidCardServiceKind? serviceKind)
    {
        _showChargeOverlay = showChargeOverlay;
        _availableChargeAmount = Math.Max(0, availableChargeAmount);
        ServiceKind = serviceKind;

        ChargePrepaidWalletCommand = new AsyncRelayCommand(() => _showChargeOverlay(PrepaidCardWalletKind.Prepaid));
        ChargeTrafficWalletCommand = new AsyncRelayCommand(() => _showChargeOverlay(PrepaidCardWalletKind.Traffic));
        IsPrimaryEnabled = false;
    }

    public PrepaidCardServiceKind? ServiceKind { get; }

    public bool ShowCardPurchaseNotice => ServiceKind == PrepaidCardServiceKind.PurchaseAndCharge;

    public string CardPurchaseNoticeText => "카드 구매비 ₩5,000이 차감된 금액입니다.";

    public IAsyncRelayCommand ChargePrepaidWalletCommand { get; }

    public IAsyncRelayCommand ChargeTrafficWalletCommand { get; }

    public int AvailableChargeAmount => _availableChargeAmount;

    public string AvailableChargeAmountText => FormatKrw(AvailableChargeAmount);

    public string AvailableChargeAmountNumberText => AvailableChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string AvailableChargeAmountCurrencyText => CurrencyCode;

    public string PrepaidWalletAmountText => FormatKrw(PrepaidWalletAmount);

    public string PrepaidWalletAmountNumberText => PrepaidWalletAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string PrepaidWalletAmountCurrencyText => CurrencyCode;

    public string TrafficWalletAmountText => FormatKrw(TrafficWalletAmount);

    public string TrafficWalletAmountNumberText => TrafficWalletAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string TrafficWalletAmountCurrencyText => CurrencyCode;

    public int CashPayoutAmount => Math.Max(0, AvailableChargeAmount - PrepaidWalletAmount - TrafficWalletAmount);

    public string CashPayoutAmountText => FormatKrw(CashPayoutAmount);

    public string CashPayoutAmountNumberText => CashPayoutAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string CashPayoutAmountCurrencyText => CurrencyCode;

    public int GetWalletAmount(PrepaidCardWalletKind walletKind)
        => walletKind == PrepaidCardWalletKind.Prepaid ? PrepaidWalletAmount : TrafficWalletAmount;

    public int GetOtherWalletAmount(PrepaidCardWalletKind walletKind)
        => walletKind == PrepaidCardWalletKind.Prepaid ? TrafficWalletAmount : PrepaidWalletAmount;

    public int GetMaxChargeableAmount(PrepaidCardWalletKind walletKind)
        => Math.Max(0, Math.Min(WalletMaxAmount, AvailableChargeAmount - GetOtherWalletAmount(walletKind)));

    public void SetWalletAmount(PrepaidCardWalletKind walletKind, int amount)
    {
        var clampedAmount = Math.Clamp(amount, 0, GetMaxChargeableAmount(walletKind));

        if (walletKind == PrepaidCardWalletKind.Prepaid)
            PrepaidWalletAmount = clampedAmount;
        else
            TrafficWalletAmount = clampedAmount;
    }

    partial void OnPrepaidWalletAmountChanged(int value)
        => NotifyAmountsChanged();

    partial void OnTrafficWalletAmountChanged(int value)
        => NotifyAmountsChanged();

    private void NotifyAmountsChanged()
    {
        OnPropertyChanged(nameof(PrepaidWalletAmountText));
        OnPropertyChanged(nameof(PrepaidWalletAmountNumberText));
        OnPropertyChanged(nameof(PrepaidWalletAmountCurrencyText));
        OnPropertyChanged(nameof(TrafficWalletAmountText));
        OnPropertyChanged(nameof(TrafficWalletAmountNumberText));
        OnPropertyChanged(nameof(TrafficWalletAmountCurrencyText));
        OnPropertyChanged(nameof(CashPayoutAmount));
        OnPropertyChanged(nameof(CashPayoutAmountText));
        OnPropertyChanged(nameof(CashPayoutAmountNumberText));
        OnPropertyChanged(nameof(CashPayoutAmountCurrencyText));
        IsPrimaryEnabled = PrepaidWalletAmount + TrafficWalletAmount > 0;
    }

    private static string FormatKrw(int amount)
        => string.Create(CultureInfo.InvariantCulture, $"{amount:N0} {CurrencyCode}");
}
