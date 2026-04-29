using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.Steps;
using System.Globalization;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed partial class PrepaidCardEasyPayAmountEntryStepViewModel : ExchangeStepViewModelBase
{
    private const int DefaultTrafficWalletBalance = 0;
    private const int DefaultMaxChargeableAmount = 500_000;
    private const string CurrencyCode = "KRW";

    [ObservableProperty]
    private int trafficWalletBalance = DefaultTrafficWalletBalance;

    [ObservableProperty]
    private int chargeAmount;

    public PrepaidCardEasyPayAmountEntryStepViewModel()
    {
        MaxChargeableAmount = DefaultMaxChargeableAmount;

        AddOneThousandCommand = new RelayCommand(() => AddAmount(1_000));
        AddFiveThousandCommand = new RelayCommand(() => AddAmount(5_000));
        AddTenThousandCommand = new RelayCommand(() => AddAmount(10_000));
        AddFiftyThousandCommand = new RelayCommand(() => AddAmount(50_000));
        AddMaxCommand = new RelayCommand(() => ChargeAmount = MaxChargeableAmount);
        ResetCommand = new RelayCommand(() => ChargeAmount = 0);

        IsPrimaryEnabled = false;
    }

    public int MaxChargeableAmount { get; }

    public string TrafficWalletBalanceNumberText => TrafficWalletBalance.ToString("N0", CultureInfo.InvariantCulture);

    public string TrafficWalletBalanceCurrencyText => CurrencyCode;

    public string ChargeAmountNumberText => ChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string ChargeAmountCurrencyText => CurrencyCode;

    public string MaxChargeableAmountNumberText => MaxChargeableAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string MaxChargeableAmountCurrencyText => CurrencyCode;

    public IRelayCommand AddOneThousandCommand { get; }

    public IRelayCommand AddFiveThousandCommand { get; }

    public IRelayCommand AddTenThousandCommand { get; }

    public IRelayCommand AddFiftyThousandCommand { get; }

    public IRelayCommand AddMaxCommand { get; }

    public IRelayCommand ResetCommand { get; }

    partial void OnChargeAmountChanged(int value)
    {
        OnPropertyChanged(nameof(ChargeAmountNumberText));
        OnPropertyChanged(nameof(ChargeAmountCurrencyText));
        IsPrimaryEnabled = ChargeAmount > 0;
    }

    private void AddAmount(int amountToAdd)
        => ChargeAmount = Math.Min(MaxChargeableAmount, ChargeAmount + amountToAdd);
}
