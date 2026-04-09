using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.Application.Services.Devices.Withdrawal;

namespace Kiosk.ViewModels.Steps;

public sealed class DispensingStepViewModel : ExchangeStepViewModelBase
{
    public DispensingStepViewModel(
        string targetCurrencyCode,
        decimal targetAmount,
        IReadOnlyList<WithdrawalSlotBalance> slots)
    {
        Title = string.Empty;

        var currency = string.IsNullOrWhiteSpace(targetCurrencyCode)
            ? "KRW"
            : targetCurrencyCode.ToUpperInvariant();

        TargetCurrencyCode = currency;
        PrimaryMessage = $"{GetCurrencyLabel(currency)}가 출금중입니다";
        SecondaryMessage = "잠시만 기다려주세요...";

        DisplayNoteImagePaths = ResolveDisplayNoteImages(currency);

        var plan = ExchangeWithdrawalSession.CreatePlan(currency, targetAmount, slots);
        if (!plan.Success)
        {
            PlanSummary = plan.ErrorMessage ?? "출금 계획을 만들 수 없습니다.";
            PlanLines = ["계획 없음"];
            CommandLines = ["DISPENSE 요청 준비 불가"];
            return;
        }

        PlanSummary = $"{targetAmount.ToString("0.##", CultureInfo.InvariantCulture)} {currency} 출금 계획";
        PlanLines = new ObservableCollection<string>(
            plan.Allocations.Select(x =>
                $"{x.DeviceId} / Slot {x.Slot} / {x.Denomination.ToString("0.##", CultureInfo.InvariantCulture)} x {x.Count}"));

        CommandLines = new ObservableCollection<string>(
            plan.Allocations
                .GroupBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var requestText = string.Join(", ", g
                        .GroupBy(x => x.Slot)
                        .OrderBy(x => x.Key)
                        .Select(x => $"S{x.Key}:{x.Sum(v => v.Count)}"));
                    return $"{g.Key} => {requestText}";
                }));
    }

    public string TargetCurrencyCode { get; }
    public string PrimaryMessage { get; }
    public string SecondaryMessage { get; }
    public string PlanSummary { get; }
    public IReadOnlyList<string> DisplayNoteImagePaths { get; }
    public ObservableCollection<string> PlanLines { get; }
    public ObservableCollection<string> CommandLines { get; }

    private static IReadOnlyList<string> ResolveDisplayNoteImages(string currencyCode)
    {
        var directory = ResolveAssetDirectory("Image\\Denomination");
        if (directory is null || !directory.Exists)
            return [];

        return directory
            .GetFiles($"{currencyCode}_*.png", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => ParseDenomination(file.Name))
            .Take(3)
            .Select(file => file.FullName)
            .ToArray();
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

    private static string GetCurrencyLabel(string currencyCode)
        => currencyCode switch
        {
            "KRW" => "원화",
            _ => currencyCode
        };
}
