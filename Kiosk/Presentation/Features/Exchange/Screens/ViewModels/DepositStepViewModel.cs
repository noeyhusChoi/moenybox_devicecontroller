using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.Application.Services.Exchange;

namespace Kiosk.ViewModels.Steps;

public sealed partial class DepositStepViewModel : ExchangeStepViewModelBase, IDepositProgressConsumer
{
    public DepositStepViewModel(
        string sourceCurrencyCode,
        string targetCurrencyCode,
        decimal depositAmount,
        decimal previewExchangeAmount,
        decimal exchangeRate,
        DepositLimitSnapshot? depositLimit)
    {
        Title = string.Empty;

        var sourceCurrency = string.IsNullOrWhiteSpace(sourceCurrencyCode)
            ? "USD"
            : sourceCurrencyCode.ToUpperInvariant();
        var targetCurrency = string.IsNullOrWhiteSpace(targetCurrencyCode)
            ? "KRW"
            : targetCurrencyCode.ToUpperInvariant();

        Title = $"{sourceCurrency}를 입금해주세요";
        SourceCurrencyCode = sourceCurrency;
        TargetCurrencyCode = targetCurrency;
        FlagImagePath = CreateAssetPath("Flag", $"{SourceCurrencyCode}.png");
        GuideImagePath = CreateAssetPath("Gif\\DepositGuide", $"Guide_Deposit_{SourceCurrencyCode}.gif");
        AcceptedDenominationImagePaths = ResolveAcceptedDenominations(SourceCurrencyCode);

        DepositAmountText = depositAmount.ToString("0.##");
        ExchangeAmountText = previewExchangeAmount.ToString("0.00");
        ExchangeRateText = exchangeRate.ToString("0.00");
        StatusMessage = "외화를 투입해주세요.";

        DailyMaximumAmountText = ConvertKrwLimitForDisplay(depositLimit?.DailyMaximumAmount, exchangeRate).ToString("0.00");
        DailyRemainingMaximumAmountText = ConvertKrwLimitForDisplay(depositLimit?.DailyRemainingMaximumAmount, exchangeRate).ToString("0.00");
        DailyAvailableExchangeAmountText = ConvertKrwLimitForDisplay(depositLimit?.PerTransactionMaximumAmount, exchangeRate).ToString("0.00");
    }

    public string SourceCurrencyCode { get; }
    public string TargetCurrencyCode { get; }
    public string? FlagImagePath { get; }
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
    [ObservableProperty]
    private string statusMessage = string.Empty;

    public void ApplyDepositProgress(ExchangeDepositProgressChangedEventArgs progress)
    {
        DepositAmountText = progress.ApprovedDepositAmount.ToString("0.##");
        ExchangeAmountText = progress.ExchangedAmount.ToString("0.00");
        StatusMessage = progress.StatusMessage;
    }

    private static IReadOnlyList<string> ResolveAcceptedDenominations(string currencyCode)
    {
        var directory = ResolveAssetDirectory("Image\\Denomination");
        if (directory is null || !directory.Exists)
            return [];

        return directory
            .GetFiles($"{currencyCode.ToUpperInvariant()}_*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(file => ParseDenomination(file.Name))
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
