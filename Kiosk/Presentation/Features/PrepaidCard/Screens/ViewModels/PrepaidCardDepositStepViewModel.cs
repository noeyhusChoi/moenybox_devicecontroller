using CommunityToolkit.Mvvm.ComponentModel;
using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.ViewModels.Steps;
using System.Globalization;
using System.IO;

namespace Kiosk.ViewModels.PrepaidCard;

public sealed partial class PrepaidCardDepositStepViewModel : ExchangeStepViewModelBase, IDepositProgressConsumer
{
    public PrepaidCardDepositStepViewModel(
        string sourceCurrencyCode,
        decimal exchangeRate,
        PrepaidCardServiceKind? serviceKind)
    {
        var sourceCurrency = string.IsNullOrWhiteSpace(sourceCurrencyCode)
            ? "USD"
            : sourceCurrencyCode.ToUpperInvariant();

        Title = $"{sourceCurrency}를 입금해주세요";
        SourceCurrencyCode = sourceCurrency;
        ExchangeRateText = exchangeRate.ToString("#,0.##", CultureInfo.InvariantCulture);
        SourceFlagPath = CreateFlagPath(sourceCurrency);
        TargetFlagPath = CreateFlagPath("KOR");
        GuideImagePath = CreateAssetPath("Gif\\DepositGuide", $"Guide_Deposit_{sourceCurrency}.gif");
        AcceptedDenominationImagePaths = ResolveAcceptedDenominations(sourceCurrency);
        DepositAmountText = "0";
        ExchangeAmountText = "0.00";
        CardPurchaseAmountText = serviceKind == PrepaidCardServiceKind.PurchaseAndCharge ? "5,000" : "0";
        ShowCardPurchaseAmount = serviceKind == PrepaidCardServiceKind.PurchaseAndCharge;
        StatusMessage = "?명솕瑜??ъ엯?댁＜?몄슂.";
    }

    public string SourceCurrencyCode { get; }

    public string ExchangeRateText { get; }

    public string? SourceFlagPath { get; }

    public string? TargetFlagPath { get; }

    public string GuideImagePath { get; }

    public IReadOnlyList<string> AcceptedDenominationImagePaths { get; }

    [ObservableProperty]
    private string depositAmountText = "0";

    [ObservableProperty]
    private string exchangeAmountText = "0.00";

    public string CardPurchaseAmountText { get; }

    public bool ShowCardPurchaseAmount { get; }

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public void ApplyDepositProgress(ExchangeDepositProgressChangedEventArgs progress)
    {
        DepositAmountText = progress.ApprovedDepositAmount.ToString("0.##", CultureInfo.InvariantCulture);
        ExchangeAmountText = progress.ExchangedAmount.ToString("0.00", CultureInfo.InvariantCulture);
        StatusMessage = progress.StatusMessage;
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

    private static string? CreateFlagPath(string assetCode)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Assets", "Flag", $"{assetCode}.png");
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
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
}
