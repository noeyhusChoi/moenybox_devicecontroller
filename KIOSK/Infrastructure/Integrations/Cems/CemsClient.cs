using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Kiosk.Infrastructure.Integrations.Cems.Models;
using Kiosk.Infrastructure.Integrations.Cems.Requests;
using Kiosk.Infrastructure.Integrations.Cems.Responses;
using Kiosk.Infrastructure.Integrations.Common;

namespace Kiosk.Infrastructure.Integrations.Cems;

public sealed class CemsClient : ICemsClient
{
    private readonly IHttpExecutor _httpExecutor;
    private readonly IProviderConfigResolver _configResolver;

    public CemsClient(IHttpExecutor httpExecutor, IProviderConfigResolver configResolver)
    {
        _httpExecutor = httpExecutor;
        _configResolver = configResolver;
    }

    public async Task<CemsCommandResult<CemsGetRateResponse>> GetRateAsync(CemsGetRateRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C010", new Dictionary<string, string>
        {
            ["currency"] = request.CurrencyCode
        }, ct);

        if (!execution.Success)
            return Failure<CemsGetRateResponse>(execution);

        var parsed = ParseEnvelope(execution.RawBody);
        var response = new CemsGetRateResponse(
            parsed.Result,
            parsed.ErrorCode,
            parsed.Fields.GetValueOrDefault("currency"),
            TryParseDecimal(parsed.Fields.GetValueOrDefault("rate")),
            parsed.Fields);

        return BuildResult(execution, response, parsed);
    }

    public async Task<CemsCommandResult<CemsGetRateAllResponse>> GetRateAllAsync(CemsGetRateAllRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C011", null, ct);
        if (!execution.Success)
            return Failure<CemsGetRateAllResponse>(execution);

        var parsed = ParseRateAllEnvelope(execution.RawBody);
        var response = new CemsGetRateAllResponse(parsed.Result, parsed.ErrorCode, parsed.Rates, parsed.Fields);
        return BuildResult(execution, response, new CemsEnvelope(parsed.Result, parsed.ErrorCode, parsed.Fields));
    }

    public async Task<CemsCommandResult<CemsCheckLimitResponse>> CheckLimitAsync(CemsCheckLimitRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C020", new Dictionary<string, string>
        {
            ["number"] = request.CustomerNumber
        }, ct);

        if (!execution.Success)
            return Failure<CemsCheckLimitResponse>(execution);

        var parsed = ParseEnvelope(execution.RawBody);
        var response = new CemsCheckLimitResponse(
            parsed.Result,
            parsed.ErrorCode,
            TryParseDecimal(parsed.Fields.GetValueOrDefault("limit_amt")),
            TryParseDecimal(parsed.Fields.GetValueOrDefault("used_amt")),
            parsed.Fields);

        return BuildResult(execution, response, parsed);
    }

    public async Task<CemsCommandResult<CemsRegisterTransactionResponse>> RegisterTransactionAsync(CemsRegisterTransactionRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C030", BuildTransactionParameters(request.Transaction), ct);
        if (!execution.Success)
            return Failure<CemsRegisterTransactionResponse>(execution);

        var parsed = ParseEnvelope(execution.RawBody);
        var response = new CemsRegisterTransactionResponse(parsed.Result, parsed.ErrorCode, parsed.Fields);
        return BuildResult(execution, response, parsed);
    }

    public async Task<CemsCommandResult<CemsSetCashResponse>> SetCashAsync(CemsSetCashRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C040", BuildSetCashParameters(request.Cassettes), ct);
        if (!execution.Success)
            return Failure<CemsSetCashResponse>(execution);

        var parsed = ParseEnvelope(execution.RawBody);
        var response = new CemsSetCashResponse(parsed.Result, parsed.ErrorCode, parsed.Fields);
        return BuildResult(execution, response, parsed);
    }

    public async Task<CemsCommandResult<CemsPullCashResponse>> PullCashAsync(CemsPullCashRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C070", null, ct);
        if (!execution.Success)
            return Failure<CemsPullCashResponse>(execution);

        var parsed = ParseEnvelope(execution.RawBody);
        var response = new CemsPullCashResponse(parsed.Result, parsed.ErrorCode, parsed.Fields);
        return BuildResult(execution, response, parsed);
    }

    public async Task<CemsCommandResult<CemsIncidentResponse>> ReportErrorAsync(CemsReportErrorRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C060", new Dictionary<string, string>
        {
            ["dt"] = request.OccurredAt.ToString("yyyyMMddHHmmss"),
            ["error"] = request.Message
        }, ct);

        if (!execution.Success)
            return Failure<CemsIncidentResponse>(execution);

        var parsed = ParseEnvelope(execution.RawBody);
        var response = new CemsIncidentResponse(parsed.Result, parsed.ErrorCode, parsed.Fields);
        return BuildResult(execution, response, parsed);
    }

    public async Task<CemsCommandResult<CemsSmsResponse>> SendSmsAsync(CemsSendSmsRequest request, CancellationToken ct = default)
    {
        var execution = await ExecuteAsync("C090", new Dictionary<string, string>
        {
            ["dt"] = request.OccurredAt.ToString("yyyyMMddHHmmss"),
            ["type"] = request.Type,
            ["error"] = request.Message
        }, ct);

        if (!execution.Success)
            return Failure<CemsSmsResponse>(execution);

        var parsed = ParseEnvelope(execution.RawBody);
        var response = new CemsSmsResponse(parsed.Result, parsed.ErrorCode, parsed.Fields);
        return BuildResult(execution, response, parsed);
    }

    private async Task<HttpExecutionResult> ExecuteAsync(string command, Dictionary<string, string>? parameters, CancellationToken ct)
    {
        var config = _configResolver.GetRequired("CEMS");
        var correlationId = Guid.NewGuid().ToString("N");
        var query = parameters is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(parameters);

        query["cmd"] = command;
        query["key"] = config.ApiKey;

        var queryString = string.Join("&", query.Select(static item =>
            $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        var requestUri = $"{config.BaseUrl.TrimEnd('/')}/api/cmdV2.php?{queryString}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return await _httpExecutor.SendAsync(
            httpRequest,
            new HttpExecutionOptions(config.Timeout, config.RetryCount, correlationId),
            ct);
    }

    private static CemsEnvelope ParseEnvelope(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            var result = root.TryGetProperty("result", out var resultProp) && resultProp.GetBoolean();
            var errorCode = root.TryGetProperty("ecode", out var errorProp) ? errorProp.GetString() : null;
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name is "result" or "ecode")
                    continue;

                fields[property.Name] = property.Value.ToString();
            }

            return new CemsEnvelope(result, errorCode, fields);
        }
        catch
        {
            return new CemsEnvelope(false, "PARSE_ERROR", new Dictionary<string, string?>());
        }
    }

    private static CemsRateAllEnvelope ParseRateAllEnvelope(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            var result = root.TryGetProperty("result", out var resultProp) && resultProp.GetBoolean();
            var errorCode = root.TryGetProperty("ecode", out var errorProp) ? errorProp.GetString() : null;
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var rates = new Dictionary<string, CurrencyRate>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataProp.EnumerateArray())
                {
                    if (!item.TryGetProperty("currency", out var currencyProp))
                        continue;

                    var currency = currencyProp.GetString();
                    if (string.IsNullOrWhiteSpace(currency))
                        continue;

                    var baseValue = GetRateValue(item, "base", "1");
                    var sellValue = GetRateValue(item, "sell", "2");
                    var buyValue = GetRateValue(item, "buy", "3");

                    rates[currency] = new CurrencyRate(
                        TryParseDecimal(baseValue),
                        TryParseDecimal(sellValue),
                        TryParseDecimal(buyValue));
                    fields[currency] = baseValue;
                }
            }

            return new CemsRateAllEnvelope(result, errorCode, rates, fields);
        }
        catch
        {
            return new CemsRateAllEnvelope(
                false,
                "PARSE_ERROR",
                new Dictionary<string, CurrencyRate>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));
        }
    }

    private static string? GetRateValue(JsonElement item, string propertyName, string legacyPropertyName)
        => item.TryGetProperty(propertyName, out var valueProp)
            ? valueProp.ToString()
            : item.TryGetProperty(legacyPropertyName, out var legacyProp)
                ? legacyProp.ToString()
                : null;

    private static CemsCommandResult<T> BuildResult<T>(HttpExecutionResult execution, T response, CemsEnvelope parsed)
    {
        var error = parsed.Result
            ? null
            : new CemsError(parsed.ErrorCode ?? "CEMS_ERROR", parsed.ErrorCode ?? "CEMS rejected request.", false);

        return new CemsCommandResult<T>(
            parsed.Result,
            response,
            error,
            execution.StatusCode,
            execution.RawBody,
            execution.CorrelationId);
    }

    private static CemsCommandResult<T> Failure<T>(HttpExecutionResult execution)
        => new(
            false,
            default,
            new CemsError("HTTP_ERROR", execution.Exception?.Message ?? execution.RawBody, execution.StatusCode is >= 500),
            execution.StatusCode,
            execution.RawBody,
            execution.CorrelationId);

    private static decimal? TryParseDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static Dictionary<string, string> BuildTransactionParameters(Kiosk.Domain.Entities.TransactionModelV2 transaction)
    {
        var dict = new Dictionary<string, string>
        {
            ["dt"] = transaction.TransactionDate.ToString("yyyyMMddHHmm"),
            ["gubun"] = transaction.TransactionType,
            ["currency_code"] = transaction.CurrencyPair.BaseCurrency,
            ["unique_key"] = string.IsNullOrWhiteSpace(transaction.TransactionID) ? Guid.NewGuid().ToString("N") : transaction.TransactionID,
            ["KIOSK_PID"] = "1",
            ["rate"] = transaction.CurrencyPair.Rate.ToString(CultureInfo.InvariantCulture),
            ["input_money"] = transaction.SourceDepositedTotal.ToString(CultureInfo.InvariantCulture),
            ["output_money"] = transaction.TargetComputedAmount.ToString(CultureInfo.InvariantCulture),
            ["give_change"] = transaction.SourceChangeAmount.ToString(CultureInfo.InvariantCulture),
            ["identity"] = transaction.Customer.IdType,
            ["number"] = transaction.Customer.CustomerNumber,
            ["name"] = transaction.Customer.CustomerName,
            ["nation"] = transaction.Customer.CustomerNationality
        };

        var payouts = transaction.TargetPayouts
            .Where(x => x.CurrencyCode != "KRW" && x.SucceededCount > 0)
            .OrderByDescending(x => x.Denomination)
            .Take(12)
            .ToList();

        for (var i = 0; i < 12; i++)
        {
            if (i < payouts.Count)
            {
                dict[$"c{i + 1}"] = payouts[i].Denomination.ToString(CultureInfo.InvariantCulture);
                dict[$"qty{i + 1}"] = payouts[i].SucceededCount.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                dict[$"c{i + 1}"] = "0";
                dict[$"qty{i + 1}"] = "0";
            }
        }

        dict["krw1"] = SumBySuccessCount(transaction, 50_000m);
        dict["krw2"] = SumBySuccessCount(transaction, 10_000m);
        dict["krw3"] = SumBySuccessCount(transaction, 5_000m);
        dict["krw4"] = SumBySuccessCount(transaction, 1_000m);
        dict["krw5"] = SumBySuccessCount(transaction, 500m);
        dict["krw6"] = SumBySuccessCount(transaction, 100m);
        dict["krw7"] = SumBySuccessCount(transaction, 50m);
        dict["krw8"] = SumBySuccessCount(transaction, 10m);

        dict["withdrawal_error"] = bool.FalseString;
        dict["error_amount"] = transaction.TargetFailedTotalAmount.ToString(CultureInfo.InvariantCulture);
        dict["error_change_amount"] = transaction.ChangeFailedTotalAmount.ToString(CultureInfo.InvariantCulture);
        dict["reject_krw_1"] = SumRejectCount(transaction, 50_000m);
        dict["reject_krw_2"] = SumRejectCount(transaction, 10_000m);
        dict["reject_krw_3"] = SumRejectCount(transaction, 5_000m);
        dict["reject_krw_4"] = SumRejectCount(transaction, 1_000m);
        dict["reject_for"] = transaction.TargetPayouts.Concat(transaction.ChangePayouts)
            .Where(x => x.CurrencyCode != "KRW")
            .Sum(x => x.RejectCount)
            .ToString(CultureInfo.InvariantCulture);

        return dict;
    }

    private static Dictionary<string, string> BuildSetCashParameters(IReadOnlySet<Kiosk.Application.Services.WithdrawalCassette> cash)
    {
        return new Dictionary<string, string>
        {
            ["krw_1"] = SumByCassette(cash, "KRW", 50_000m),
            ["krw_2"] = SumByCassette(cash, "KRW", 10_000m),
            ["krw_3"] = SumByCassette(cash, "KRW", 5_000m),
            ["krw_4"] = SumByCassette(cash, "KRW", 1_000m),
            ["krw_5"] = SumByCassette(cash, "KRW", 500m),
            ["krw_6"] = SumByCassette(cash, "KRW", 100m),
            ["krw_7"] = SumByCassette(cash, "KRW", 50m),
            ["krw_8"] = SumByCassette(cash, "KRW", 10m),
            ["for_1"] = SumByCassette(cash, "USD"),
            ["for_2"] = SumByCassette(cash, "EUR"),
            ["for_3"] = SumByCassette(cash, "CNY"),
            ["for_4"] = SumByCassette(cash, "JPY"),
            ["for_5"] = SumByCassette(cash, "HKD"),
            ["for_6"] = SumByCassette(cash, "TWD"),
            ["for_7"] = SumByCassette(cash, "PHP"),
            ["for_8"] = SumByCassette(cash, "VND")
        };
    }

    private static string SumByCassette(IReadOnlySet<Kiosk.Application.Services.WithdrawalCassette> cash, string currency, decimal? denomination = null)
    {
        var query = cash.Where(x => string.Equals(x.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase));
        if (denomination.HasValue)
            query = query.Where(x => x.Denomination == denomination.Value);

        return query.Sum(x => x.Count).ToString(CultureInfo.InvariantCulture);
    }

    private static string SumBySuccessCount(Kiosk.Domain.Entities.TransactionModelV2 transaction, decimal denomination)
    {
        var sum = transaction.TargetPayouts.Concat(transaction.ChangePayouts)
            .Where(x => x.CurrencyCode == "KRW" && x.Denomination == denomination)
            .Sum(x => x.SucceededCount);
        return sum.ToString(CultureInfo.InvariantCulture);
    }

    private static string SumRejectCount(Kiosk.Domain.Entities.TransactionModelV2 transaction, decimal denomination)
    {
        var sum = transaction.TargetPayouts.Concat(transaction.ChangePayouts)
            .Where(x => x.CurrencyCode == "KRW" && x.Denomination == denomination)
            .Sum(x => x.RejectCount);
        return sum.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record CemsEnvelope(
        bool Result,
        string? ErrorCode,
        IReadOnlyDictionary<string, string?> Fields);

    private sealed record CemsRateAllEnvelope(
        bool Result,
        string? ErrorCode,
        IReadOnlyDictionary<string, CurrencyRate> Rates,
        IReadOnlyDictionary<string, string?> Fields);
}
