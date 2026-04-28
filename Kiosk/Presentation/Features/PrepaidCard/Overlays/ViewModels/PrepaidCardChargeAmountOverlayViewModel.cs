using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.ViewModels.PrepaidCard;
using System.Globalization;

namespace Kiosk.ViewModels.Overlays;

public sealed partial class PrepaidCardChargeAmountOverlayViewModel : ObservableObject
{
    private const string CurrencyCode = "KRW";
    private readonly Action<int> _applyAmount;
    private readonly IRelayCommand _closeCommand;

    [ObservableProperty]
    private int amount;

    public PrepaidCardChargeAmountOverlayViewModel(
        PrepaidCardWalletKind walletKind,
        int initialAmount,
        int availableChargeAmount,
        int maxChargeableAmount,
        Action<int> applyAmount,
        IRelayCommand closeCommand)
    {
        WalletName = walletKind == PrepaidCardWalletKind.Prepaid ? "선불지갑" : "교통지갑";
        AvailableChargeAmount = availableChargeAmount;
        MaxChargeableAmount = Math.Max(0, maxChargeableAmount);
        _applyAmount = applyAmount;
        _closeCommand = closeCommand;

        AddOneThousandCommand = new RelayCommand(() => AddAmount(1_000));
        AddFiveThousandCommand = new RelayCommand(() => AddAmount(5_000));
        AddTenThousandCommand = new RelayCommand(() => AddAmount(10_000));
        AddFiftyThousandCommand = new RelayCommand(() => AddAmount(50_000));
        AddMaxCommand = new RelayCommand(() => Amount = MaxChargeableAmount);
        ResetCommand = new RelayCommand(() => Amount = 0);
        CancelCommand = closeCommand;
        CompleteCommand = new RelayCommand(Complete);

        Amount = Math.Clamp(initialAmount, 0, MaxChargeableAmount);
    }

    public string WalletName { get; }

    public string Title => $"{WalletName} 충전";

    public int AvailableChargeAmount { get; }

    public int MaxChargeableAmount { get; }

    public string AmountNumberText => Amount.ToString("N0", CultureInfo.InvariantCulture);

    public string AmountCurrencyText => CurrencyCode;

    public string MaxChargeableAmountText => FormatKrw(MaxChargeableAmount);

    public string MaxChargeableDescriptionText => $"최대 충전 가능 금액: {MaxChargeableAmountText}";

    public string AvailableChargeAmountText => FormatKrw(AvailableChargeAmount);

    public string AvailableChargeAmountNumberText => AvailableChargeAmount.ToString("N0", CultureInfo.InvariantCulture);

    public string AvailableChargeAmountCurrencyText => CurrencyCode;

    public IRelayCommand AddOneThousandCommand { get; }

    public IRelayCommand AddFiveThousandCommand { get; }

    public IRelayCommand AddTenThousandCommand { get; }

    public IRelayCommand AddFiftyThousandCommand { get; }

    public IRelayCommand AddMaxCommand { get; }

    public IRelayCommand ResetCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand CompleteCommand { get; }

    partial void OnAmountChanged(int value)
    {
        OnPropertyChanged(nameof(AmountNumberText));
        OnPropertyChanged(nameof(AmountCurrencyText));
    }

    private void AddAmount(int amountToAdd)
        => Amount = Math.Min(MaxChargeableAmount, Amount + amountToAdd);

    private void Complete()
    {
        _applyAmount(Amount);
        _closeCommand.Execute(null);
    }

    private static string FormatKrw(int amount)
        => string.Create(CultureInfo.InvariantCulture, $"{amount:N0} {CurrencyCode}");
}
