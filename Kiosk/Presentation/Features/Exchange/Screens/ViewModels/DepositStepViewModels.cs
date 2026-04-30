using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.Application.Services.Exchange;
using Kiosk.ViewModels;
using Kiosk.ViewModels.PrepaidCard;

namespace Kiosk.ViewModels.Steps;

public enum DepositInfoVariant
{
    Exchange,
    PrepaidCard
}

public sealed record DepositInfoRowItem(
    string Label,
    string AmountText,
    bool HasTopDivider,
    double RowHeight);

public abstract partial class DepositStepViewModelBase : ExchangeStepViewModelBase, IDepositProgressConsumer
{
    protected DepositStepViewModelBase(
        string sourceCurrencyCode,
        string targetCurrencyCode,
        decimal depositAmount,
        decimal previewExchangeAmount,
        decimal exchangeRate,
        DepositLimitSnapshot? depositLimit,
        DepositInfoVariant infoVariant = DepositInfoVariant.Exchange,
        PrepaidCardServiceKind? prepaidCardServiceKind = null,
        PrepaidCardWalletKind prepaidCardWalletKind = PrepaidCardWalletKind.Prepaid,
        IRelayCommand? showPrepaidLimitInfoCommand = null)
    {
        Title = string.Empty;

        var sourceCurrency = string.IsNullOrWhiteSpace(sourceCurrencyCode)
            ? "USD"
            : sourceCurrencyCode.ToUpperInvariant();
        var targetCurrency = string.IsNullOrWhiteSpace(targetCurrencyCode)
            ? "KRW"
            : targetCurrencyCode.ToUpperInvariant();

        Title = $"{sourceCurrency}를 입금해주세요";
        InfoVariant = infoVariant;
        SourceCurrencyCode = sourceCurrency;
        TargetCurrencyCode = targetCurrency;
        IsBaseCurrencyDeposit = string.Equals(SourceCurrencyCode, TargetCurrencyCode, StringComparison.OrdinalIgnoreCase);
        PrepaidCardWalletKind = prepaidCardWalletKind;
        SourceFlagPath = CreateAssetPath("Flag", $"{ResolveFlagAssetCode(SourceCurrencyCode)}.png");
        TargetFlagPath = CreateAssetPath("Flag", $"{ResolveFlagAssetCode(TargetCurrencyCode)}.png");
        GuideImagePath = CreateAssetPath("Gif\\DepositGuide", $"Guide_Deposit_{SourceCurrencyCode}.gif");
        AcceptedDenominationImagePaths = ResolveAcceptedDenominations(SourceCurrencyCode);

        DepositAmountText = depositAmount.ToString("0.##");
        ExchangeAmountText = previewExchangeAmount.ToString("0.##");
        ExchangeRateText = exchangeRate.ToString("#,0.##");
        StatusMessage = IsBaseCurrencyDeposit ? "원화를 투입해주세요." : "외화를 투입해주세요.";

        DailyMaximumAmountText = ConvertKrwLimitForDisplay(depositLimit?.DailyMaximumAmount, exchangeRate).ToString("0.00");
        DailyRemainingMaximumAmountText = ConvertKrwLimitForDisplay(depositLimit?.DailyRemainingMaximumAmount, exchangeRate).ToString("0.00");
        DailyAvailableExchangeAmountText = ConvertKrwLimitForDisplay(depositLimit?.PerTransactionMaximumAmount, exchangeRate).ToString("0.00");
        CardPurchaseAmountText = prepaidCardServiceKind == PrepaidCardServiceKind.PurchaseAndCharge ? "5,000" : "0";
        ShowCardPurchaseAmount = prepaidCardServiceKind == PrepaidCardServiceKind.PurchaseAndCharge;
        BaseCurrencyInfoRows = CreateBaseCurrencyInfoRows();
        ShowPrepaidLimitInfoCommand = showPrepaidLimitInfoCommand;
    }

    public DepositInfoVariant InfoVariant { get; }
    public bool ShowExchangeLimitInfo => InfoVariant == DepositInfoVariant.Exchange;
    public bool ShowPrepaidChargeInfo => InfoVariant == DepositInfoVariant.PrepaidCard;
    public bool IsBaseCurrencyDeposit { get; }
    public bool ShowExchangeRateInfo => !IsBaseCurrencyDeposit;
    public bool ShowConvertedAmountPanel => !IsBaseCurrencyDeposit;
    public string SourceCurrencyCode { get; }
    public string TargetCurrencyCode { get; }
    public PrepaidCardWalletKind PrepaidCardWalletKind { get; }
    public string WalletDisplayName => PrepaidCardWalletKind == PrepaidCardWalletKind.Traffic ? "교통지갑" : "선불지갑";
    public string WalletChargeStepLabel => $"{WalletDisplayName} 충전";
    public string WalletBalanceLabel => $"{WalletDisplayName} 잔여금액";
    public string MaxChargeableLabel => $"{WalletDisplayName} 최대 충전 가능 금액";
    public string WalletBalanceAmountText => "15,000";
    public string MaxChargeableAmountText => "35,000";
    public string EachWalletMaximumAmountText => "500,000";
    public string MaximumDepositAmountText => "1,005,000";
    public string PrimaryPrepaidInfoLabel => ShowCardPurchaseAmount ? "M-BOX 카드 구매 금액" : WalletBalanceLabel;
    public string PrimaryPrepaidInfoAmountText => ShowCardPurchaseAmount ? CardPurchaseAmountText : WalletBalanceAmountText;
    public bool ShowBaseCurrencyChargeLimitInfo => ShowPrepaidChargeInfo && IsBaseCurrencyDeposit && !ShowCardPurchaseAmount;
    public bool ShowPrepaidLimitInfoLink => ShowPrepaidChargeInfo && !ShowBaseCurrencyChargeLimitInfo;
    public IReadOnlyList<DepositInfoRowItem> BaseCurrencyInfoRows { get; }
    public string? SourceFlagPath { get; }
    public string? TargetFlagPath { get; }
    public string? GuideImagePath { get; }
    public IReadOnlyList<string> AcceptedDenominationImagePaths { get; }

    [ObservableProperty]
    private string depositAmountText = "0";

    [ObservableProperty]
    private string exchangeAmountText = "0.00";

    public string ExchangeRateText { get; }
    public string DailyMaximumAmountText { get; }
    public string DailyRemainingMaximumAmountText { get; }
    public string DailyAvailableExchangeAmountText { get; }
    public string CardPurchaseAmountText { get; }
    public bool ShowCardPurchaseAmount { get; }
    public IRelayCommand? ShowPrepaidLimitInfoCommand { get; }

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public void ApplyDepositProgress(ExchangeDepositProgressChangedEventArgs progress)
    {
        DepositAmountText = progress.ApprovedDepositAmount.ToString("0.##");
        ExchangeAmountText = progress.ExchangedAmount.ToString("0.##");
        StatusMessage = progress.StatusMessage;
    }

    private IReadOnlyList<DepositInfoRowItem> CreateBaseCurrencyInfoRows()
    {
        if (ShowCardPurchaseAmount)
        {
            return
            [
                new DepositInfoRowItem("M-BOX 카드 구매 금액", CardPurchaseAmountText, false, 87),
                new DepositInfoRowItem("각 지갑 최대 가능금액", EachWalletMaximumAmountText, true, 87),
                new DepositInfoRowItem("최대 입금 가능 금액", MaximumDepositAmountText, true, 87)
            ];
        }

        return
        [
            new DepositInfoRowItem(WalletBalanceLabel, WalletBalanceAmountText, false, 109),
            new DepositInfoRowItem(MaxChargeableLabel, MaxChargeableAmountText, true, 109)
        ];
    }

    private static IReadOnlyList<string> ResolveAcceptedDenominations(string currencyCode)
    {
        var directory = ResolveAssetDirectory("Image\\Denomination");
        if (directory is null || !directory.Exists)
            return [];

        return directory
            .GetFiles($"{currencyCode.ToUpperInvariant()}_*.png", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => ParseDenomination(file.Name))
            .Select(file => file.FullName)
            .ToArray();
    }

    private static string CreateAssetPath(string folder, string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Assets", folder, fileName);
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Assets", folder, fileName);
    }

    private static string ResolveFlagAssetCode(string currencyCode)
        => currencyCode.ToUpperInvariant() switch
        {
            "KRW" => "KOR",
            _ => currencyCode.ToUpperInvariant()
        };

    private static DirectoryInfo? ResolveAssetDirectory(string folder)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = new DirectoryInfo(Path.Combine(current.FullName, "Assets", folder));
            if (candidate.Exists)
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    private static decimal ParseDenomination(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
            return 0m;

        var separator = stem.IndexOf('_');
        if (separator < 0 || separator == stem.Length - 1)
            return 0m;

        var amountPart = stem[(separator + 1)..];
        return decimal.TryParse(amountPart, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    private static decimal ConvertKrwLimitForDisplay(decimal? amountInKrw, decimal exchangeRate)
    {
        if (amountInKrw is null or <= 0m)
            return 0m;

        if (exchangeRate <= 0m)
            return amountInKrw.Value;

        return decimal.Round(amountInKrw.Value / exchangeRate, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class ForeignCurrencyDepositStepViewModel : DepositStepViewModelBase
{
    public ForeignCurrencyDepositStepViewModel(
        string sourceCurrencyCode,
        string targetCurrencyCode,
        decimal depositAmount,
        decimal previewExchangeAmount,
        decimal exchangeRate,
        DepositLimitSnapshot? depositLimit,
        DepositInfoVariant infoVariant = DepositInfoVariant.Exchange,
        PrepaidCardServiceKind? prepaidCardServiceKind = null,
        PrepaidCardWalletKind prepaidCardWalletKind = PrepaidCardWalletKind.Prepaid,
        IRelayCommand? showPrepaidLimitInfoCommand = null)
        : base(
            sourceCurrencyCode,
            targetCurrencyCode,
            depositAmount,
            previewExchangeAmount,
            exchangeRate,
            depositLimit,
            infoVariant,
            prepaidCardServiceKind,
            prepaidCardWalletKind,
            showPrepaidLimitInfoCommand)
    {
    }
}

public sealed class BaseCurrencyDepositStepViewModel : DepositStepViewModelBase
{
    public BaseCurrencyDepositStepViewModel(
        string sourceCurrencyCode,
        string targetCurrencyCode,
        decimal depositAmount,
        decimal previewExchangeAmount,
        decimal exchangeRate,
        DepositLimitSnapshot? depositLimit,
        DepositInfoVariant infoVariant = DepositInfoVariant.Exchange,
        PrepaidCardServiceKind? prepaidCardServiceKind = null,
        PrepaidCardWalletKind prepaidCardWalletKind = PrepaidCardWalletKind.Prepaid,
        IRelayCommand? showPrepaidLimitInfoCommand = null)
        : base(
            sourceCurrencyCode,
            targetCurrencyCode,
            depositAmount,
            previewExchangeAmount,
            exchangeRate,
            depositLimit,
            infoVariant,
            prepaidCardServiceKind,
            prepaidCardWalletKind,
            showPrepaidLimitInfoCommand)
    {
    }
}
