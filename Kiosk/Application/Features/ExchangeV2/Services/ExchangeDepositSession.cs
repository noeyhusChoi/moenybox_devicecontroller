using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kiosk.Application.Services.Devices.Deposit;
using Kiosk.Application.Services.Exchange;
using Microsoft.Extensions.Logging;

namespace Kiosk.Application.Features.ExchangeV2.Services;

public sealed record ExchangeDepositSessionOptions(
    string SourceCurrencyCode,
    string TargetCurrencyCode,
    decimal ExchangeRate,
    DepositLimitSnapshot? DepositLimit);

public sealed record ExchangeDepositProgressChangedEventArgs(
    decimal ApprovedDepositAmount,
    decimal ExchangedAmount,
    decimal? LastEscrowedAmount,
    string? LastEscrowedCurrencyCode,
    bool LastAccepted,
    string StatusMessage);

public sealed record ExchangeDepositSessionStartResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface IExchangeDepositSession
{
    event EventHandler<ExchangeDepositProgressChangedEventArgs>? ProgressChanged;

    Task<ExchangeDepositSessionStartResult> StartAsync(ExchangeDepositSessionOptions options, CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}

public sealed class ExchangeDepositSession : IExchangeDepositSession
{
    private static readonly Regex CurrencyRegex = new(@"\b([A-Za-z]{3})\b", RegexOptions.Compiled);
    private static readonly Regex AmountRegex = new(@"(?<!\d)(\d+(?:\.\d+)?)(?!\d)", RegexOptions.Compiled);

    private readonly IDepositService _depositService;
    private readonly ILogger<ExchangeDepositSession> _logger;
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private readonly SemaphoreSlim _eventGate = new(1, 1);

    private ExchangeDepositSessionOptions? _options;
    private bool _running;
    private decimal _approvedDepositAmount;

    public ExchangeDepositSession(
        IDepositService depositService,
        ILogger<ExchangeDepositSession> logger)
    {
        _depositService = depositService;
        _logger = logger;
    }

    public event EventHandler<ExchangeDepositProgressChangedEventArgs>? ProgressChanged;

    public async Task<ExchangeDepositSessionStartResult> StartAsync(ExchangeDepositSessionOptions options, CancellationToken ct = default)
    {
        await _runGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_running)
            {
                return new ExchangeDepositSessionStartResult(
                    false,
                    "SYS.EXCHANGE.DEPOSIT.ALREADY_RUNNING",
                    "Exchange deposit session is already running.");
            }

            _options = options with
            {
                SourceCurrencyCode = NormalizeCurrency(options.SourceCurrencyCode),
                TargetCurrencyCode = NormalizeCurrency(options.TargetCurrencyCode)
            };
            _approvedDepositAmount = 0m;
            _running = true;
            _depositService.EventReceived += OnDepositEvent;
        }
        finally
        {
            _runGate.Release();
        }

        var start = await _depositService.StartDepositAsync(ct).ConfigureAwait(false);
        if (!start.Success)
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);

            return new ExchangeDepositSessionStartResult(
                false,
                start.ErrorCode,
                start.ErrorMessage);
        }

        PublishProgress(
            lastEscrowedAmount: null,
            lastEscrowedCurrencyCode: null,
            lastAccepted: false,
            statusMessage: "외화를 투입해주세요.");

        return new ExchangeDepositSessionStartResult(true);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        var shouldStop = false;

        await _runGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_running)
            {
                _running = false;
                shouldStop = true;
            }
        }
        finally
        {
            _runGate.Release();
        }

        if (!shouldStop)
            return;

        _depositService.EventReceived -= OnDepositEvent;

        try
        {
            await _depositService.StopDepositAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to stop deposit device at the end of deposit session.");
        }
    }

    private async void OnDepositEvent(object? sender, DepositEvent e)
    {
        if (!_running)
            return;

        try
        {
            switch (e)
            {
                case DepositEscrowedEvent escrowed:
                    await HandleEscrowedAsync(escrowed).ConfigureAwait(false);
                    break;
                case DepositFaultedEvent faulted:
                    PublishProgress(
                        lastEscrowedAmount: null,
                        lastEscrowedCurrencyCode: null,
                        lastAccepted: false,
                        statusMessage: $"입금기 오류: {faulted.Message}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unhandled deposit session event processing error.");
            PublishProgress(
                lastEscrowedAmount: null,
                lastEscrowedCurrencyCode: null,
                lastAccepted: false,
                statusMessage: "입금 처리 중 오류가 발생했습니다.");
        }
    }

    private async Task HandleEscrowedAsync(DepositEscrowedEvent escrowed)
    {
        await _eventGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_running || _options is null)
                return;

            if (!TryParseEscrowPayload(escrowed.Payload, out var note))
            {
                _logger.LogWarning("Failed to parse deposit escrow payload. payload={Payload}", escrowed.Payload);
                await _depositService.ReturnAsync(CancellationToken.None).ConfigureAwait(false);
                PublishProgress(
                    lastEscrowedAmount: null,
                    lastEscrowedCurrencyCode: null,
                    lastAccepted: false,
                    statusMessage: "권종 정보를 확인할 수 없어 반환했습니다.");
                return;
            }

            var validation = ValidateEscrow(note, _options);
            if (!validation.Accepted)
            {
                await _depositService.ReturnAsync(CancellationToken.None).ConfigureAwait(false);
                PublishProgress(
                    note.Amount,
                    note.CurrencyCode,
                    false,
                    validation.Message);
                return;
            }

            var stack = await _depositService.StackAsync(CancellationToken.None).ConfigureAwait(false);
            if (!stack.Success)
            {
                PublishProgress(
                    note.Amount,
                    note.CurrencyCode,
                    false,
                    stack.ErrorMessage ?? "입금 확정에 실패했습니다.");
                return;
            }

            _approvedDepositAmount += note.Amount;

            PublishProgress(
                note.Amount,
                note.CurrencyCode,
                true,
                $"{note.Amount:0.##} {note.CurrencyCode.ToUpperInvariant()} 입금이 승인되었습니다.");
        }
        finally
        {
            _eventGate.Release();
        }
    }

    private DepositValidationResult ValidateEscrow(DepositNote note, ExchangeDepositSessionOptions options)
    {
        if (!string.Equals(note.CurrencyCode, options.SourceCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return DepositValidationResult.Rejected($"선택한 통화({options.SourceCurrencyCode})와 다른 권종이라 반환했습니다.");
        }

        var noteAmountInKrw = decimal.Round(note.Amount * options.ExchangeRate, 2, MidpointRounding.AwayFromZero);
        var nextApprovedTotalInKrw = decimal.Round((_approvedDepositAmount + note.Amount) * options.ExchangeRate, 2, MidpointRounding.AwayFromZero);
        var limit = options.DepositLimit;

        if (limit is not null)
        {
            if (noteAmountInKrw > limit.PerTransactionMaximumAmount)
            {
                return DepositValidationResult.Rejected("1회 최대 입금 한도를 초과하여 반환했습니다.");
            }

            if (nextApprovedTotalInKrw > limit.PerTransactionMaximumAmount)
            {
                return DepositValidationResult.Rejected("누적 입금 금액이 1회 최대 입금 한도를 초과하여 반환했습니다.");
            }

            if (nextApprovedTotalInKrw > limit.DailyRemainingMaximumAmount)
            {
                return DepositValidationResult.Rejected("잔여 1일 입금 한도를 초과하여 반환했습니다.");
            }

            if (nextApprovedTotalInKrw > limit.DailyMaximumAmount)
            {
                return DepositValidationResult.Rejected("1일 최대 입금 한도를 초과하여 반환했습니다.");
            }
        }

        return DepositValidationResult.AcceptedResult();
    }

    private void PublishProgress(
        decimal? lastEscrowedAmount,
        string? lastEscrowedCurrencyCode,
        bool lastAccepted,
        string statusMessage)
    {
        var options = _options;
        if (options is null)
            return;

        var exchangedAmount = decimal.Round(_approvedDepositAmount * options.ExchangeRate, 2, MidpointRounding.AwayFromZero);

        ProgressChanged?.Invoke(
            this,
            new ExchangeDepositProgressChangedEventArgs(
                _approvedDepositAmount,
                exchangedAmount,
                lastEscrowedAmount,
                lastEscrowedCurrencyCode,
                lastAccepted,
                statusMessage));
    }

    private static bool TryParseEscrowPayload(string payload, out DepositNote note)
    {
        if (TryParseJsonPayload(payload, out note))
            return true;

        var currencyMatch = CurrencyRegex.Match(payload);
        var amountMatch = AmountRegex.Match(payload);

        if (currencyMatch.Success
            && amountMatch.Success
            && decimal.TryParse(amountMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            note = new DepositNote(currencyMatch.Groups[1].Value.ToUpperInvariant(), amount);
            return true;
        }

        note = default;
        return false;
    }

    private static bool TryParseJsonPayload(string payload, out DepositNote note)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                note = default;
                return false;
            }

            var currency = TryGetString(document.RootElement, "currency", "currencyCode", "code", "ccy", "noteCurrency");
            var amount = TryGetDecimal(document.RootElement, "amount", "denomination", "value", "billAmount");

            if (!string.IsNullOrWhiteSpace(currency) && amount is not null)
            {
                note = new DepositNote(currency.ToUpperInvariant(), amount.Value);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        note = default;
        return false;
    }

    private static string? TryGetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }

    private static decimal? TryGetDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return number;

            if (value.ValueKind == JsonValueKind.String
                && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return null;
    }

    private static string NormalizeCurrency(string currencyCode)
        => string.IsNullOrWhiteSpace(currencyCode) ? string.Empty : currencyCode.Trim().ToUpperInvariant();

    private readonly record struct DepositNote(string CurrencyCode, decimal Amount);

    private sealed record DepositValidationResult(bool Accepted, string Message)
    {
        public static DepositValidationResult AcceptedResult()
            => new(true, "입금이 승인되었습니다.");

        public static DepositValidationResult Rejected(string message)
            => new(false, message);
    }
}
