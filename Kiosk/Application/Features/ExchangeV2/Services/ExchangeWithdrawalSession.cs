using Kiosk.Application.Services.Devices.Withdrawal;

namespace Kiosk.Application.Features.ExchangeV2.Services;

public sealed record ExchangeWithdrawalSessionOptions(
    string TargetCurrencyCode,
    decimal TargetAmount,
    IReadOnlyList<WithdrawalSlotBalance> Slots);

public sealed record ExchangeWithdrawalProgressChangedEventArgs(
    decimal RequestedAmount,
    decimal PlannedAmount,
    string StatusMessage,
    IReadOnlyList<WithdrawalAllocation> Allocations);

public sealed record ExchangeWithdrawalSessionResult(
    bool Success,
    decimal RequestedAmount,
    decimal DispensedAmount,
    IReadOnlyList<WithdrawalAllocation> Allocations,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface IExchangeWithdrawalSession
{
    event EventHandler<ExchangeWithdrawalProgressChangedEventArgs>? ProgressChanged;

    Task<ExchangeWithdrawalSessionResult> ExecuteAsync(ExchangeWithdrawalSessionOptions options, CancellationToken ct = default);
}

public sealed class ExchangeWithdrawalSession : IExchangeWithdrawalSession
{
    private readonly IWithdrawalService _withdrawalService;

    public ExchangeWithdrawalSession(IWithdrawalService withdrawalService)
    {
        _withdrawalService = withdrawalService;
    }

    public event EventHandler<ExchangeWithdrawalProgressChangedEventArgs>? ProgressChanged;

    public async Task<ExchangeWithdrawalSessionResult> ExecuteAsync(ExchangeWithdrawalSessionOptions options, CancellationToken ct = default)
    {
        var availability = await _withdrawalService.GetAvailabilityAsync(ct).ConfigureAwait(false);
        if (!availability.IsAvailable)
        {
            return new ExchangeWithdrawalSessionResult(
                false,
                options.TargetAmount,
                0m,
                [],
                availability.ReasonCode,
                availability.ReasonMessage);
        }

        var plan = CreatePlan(options.TargetCurrencyCode, options.TargetAmount, options.Slots);
        if (!plan.Success)
        {
            return new ExchangeWithdrawalSessionResult(
                false,
                options.TargetAmount,
                0m,
                [],
                plan.ErrorCode,
                plan.ErrorMessage);
        }

        var start = await _withdrawalService.StartAsync(ct).ConfigureAwait(false);
        if (!start.Success)
        {
            return new ExchangeWithdrawalSessionResult(
                false,
                options.TargetAmount,
                0m,
                [],
                start.ErrorCode,
                start.ErrorMessage);
        }

        var executedAllocations = new List<WithdrawalAllocation>();

        ProgressChanged?.Invoke(
            this,
            new ExchangeWithdrawalProgressChangedEventArgs(
                options.TargetAmount,
                plan.Allocations.Sum(x => x.TotalAmount),
                "출금을 시작합니다.",
                plan.Allocations));

        foreach (var allocationGroup in plan.Allocations.GroupBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase))
        {
            var allocations = allocationGroup.ToArray();
            var result = await _withdrawalService.DispenseAsync(
                new WithdrawalDispenseCommand(allocationGroup.Key, allocations),
                ct).ConfigureAwait(false);

            if (result.DispensedAllocations.Count > 0)
                executedAllocations.AddRange(result.DispensedAllocations);

            if (!result.Success)
            {
                return new ExchangeWithdrawalSessionResult(
                    false,
                    options.TargetAmount,
                    executedAllocations.Sum(x => x.TotalAmount),
                    executedAllocations.ToArray(),
                    result.ErrorCode,
                    result.ErrorMessage);
            }

            ProgressChanged?.Invoke(
                this,
                new ExchangeWithdrawalProgressChangedEventArgs(
                    options.TargetAmount,
                    result.DispensedAllocations.Sum(x => x.TotalAmount),
                    $"{allocationGroup.Key} 출금 명령을 전송했습니다.",
                    plan.Allocations));
        }

        await _withdrawalService.StopAsync(ct).ConfigureAwait(false);

        ProgressChanged?.Invoke(
            this,
            new ExchangeWithdrawalProgressChangedEventArgs(
                options.TargetAmount,
                plan.Allocations.Sum(x => x.TotalAmount),
                "출금 계획을 완료했습니다.",
                plan.Allocations));

        return new ExchangeWithdrawalSessionResult(
            true,
            options.TargetAmount,
            executedAllocations.Sum(x => x.TotalAmount),
            executedAllocations.ToArray());
    }

    public static WithdrawalPlanResult CreatePlan(string currencyCode, decimal amount, IReadOnlyList<WithdrawalSlotBalance> slots)
    {
        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        var candidates = slots
            .Where(x => string.Equals(x.CurrencyCode, normalizedCurrency, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Count > 0 && x.Denomination > 0)
            .OrderByDescending(x => x.Denomination)
            .ThenBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Slot)
            .ToArray();

        var totalAvailableAmount = candidates.Sum(x => x.TotalAmount);
        if (totalAvailableAmount < amount)
        {
            return new WithdrawalPlanResult(
                false,
                [],
                0m,
                "SYS.EXCHANGE.WITHDRAWAL.BALANCE.INSUFFICIENT",
                "현재 시재가 부족하여 출금할 수 없습니다.");
        }

        var allocations = new List<WithdrawalAllocation>();
        var remaining = amount;

        foreach (var slot in candidates)
        {
            if (remaining <= 0)
                break;

            var maxNotes = (int)Math.Min(slot.Count, decimal.Floor(remaining / slot.Denomination));
            if (maxNotes <= 0)
                continue;

            allocations.Add(new WithdrawalAllocation(slot.DeviceId, slot.Slot, slot.CurrencyCode, slot.Denomination, maxNotes));
            remaining -= slot.Denomination * maxNotes;
        }

        if (remaining != 0)
        {
            if (allocations.Count == 0)
            {
                return new WithdrawalPlanResult(
                    false,
                    [],
                    remaining,
                    "SYS.EXCHANGE.WITHDRAWAL.PLAN.UNAVAILABLE",
                    "현재 시재로 정확한 출금 조합을 만들 수 없습니다.");
            }

            return new WithdrawalPlanResult(
                true,
                allocations,
                remaining);
        }

        return new WithdrawalPlanResult(true, allocations, 0m);
    }
}
