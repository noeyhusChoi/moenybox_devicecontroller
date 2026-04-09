using Kiosk.Infrastructure.Integrations.Cems;
using Kiosk.Infrastructure.Integrations.Cems.Models;
using Kiosk.Infrastructure.Integrations.Cems.Requests;
using Kiosk.Infrastructure.Integrations.Cems.Responses;
using Kiosk.Infrastructure.Integrations.Common;
using Kiosk.Tests.Common.Fakes;
using Kiosk.Domain.Entities;

namespace Kiosk.Tests.Unit.Infrastructure.Integrations.Cems;

public sealed class CemsClientTests
{
    [Fact]
    public async Task GetRateAsync_BuildsQueryAndParsesSuccessResponse()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"result":true,"currency":"USD","rate":"1375.25"}""",
                null,
                "corr-cems-rate")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("CEMS", "https://cems.test", "secret-key", TimeSpan.FromSeconds(5), 2));
        var sut = new CemsClient(executor, config);

        var result = await sut.GetRateAsync(new CemsGetRateRequest("USD"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CurrencyCode.Should().Be("USD");
        result.Data.Rate.Should().Be(1375.25m);
        executor.LastMethod.Should().Be(HttpMethod.Get);
        executor.LastRequestUri.Should().NotBeNull();
        executor.LastRequestUri!.ToString().Should().Be("https://cems.test/api/cmdV2.php?currency=USD&cmd=C010&key=secret-key");
        executor.LastOptions.Should().NotBeNull();
        executor.LastOptions!.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        executor.LastOptions.MaxRetry.Should().Be(2);
    }

    [Fact]
    public async Task GetRateAsync_ReturnsHttpErrorWhenExecutorFails()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                false,
                503,
                string.Empty,
                new HttpRequestException("gateway down"),
                "corr-cems-fail")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("CEMS", "https://cems.test", "secret-key", TimeSpan.FromSeconds(5), 1));
        var sut = new CemsClient(executor, config);

        var result = await sut.GetRateAsync(new CemsGetRateRequest("USD"));

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("HTTP_ERROR");
        result.Error.Retryable.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-cems-fail");
    }

    [Fact]
    public async Task CheckLimitAsync_ParsesAmountsFromResponse()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"result":true,"limit_amt":"2000","used_amt":"350"}""",
                null,
                "corr-cems-limit")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("CEMS", "https://cems.test", "secret-key", TimeSpan.FromSeconds(5), 1));
        var sut = new CemsClient(executor, config);

        var result = await sut.CheckLimitAsync(new CemsCheckLimitRequest("P123456"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Result.Should().BeTrue();
        result.Data.LimitAmount.Should().Be(2000m);
        result.Data.UsedAmount.Should().Be(350m);
        executor.LastRequestUri!.ToString().Should().Be("https://cems.test/api/cmdV2.php?number=P123456&cmd=C020&key=secret-key");
    }

    [Fact]
    public async Task GetRateAllAsync_ParsesArrayResponseUsingCurrencyAndRateFields()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"result":true,"data":[{"currency":"USD","base":"1370.5","sell":"1382.0","buy":"1361.0"},{"currency":"JPY","base":"9.12","sell":"9.2","buy":"9.05"},{"currency":"INVALID","base":"n/a","sell":"-","buy":"?"}]}""",
                null,
                "corr-cems-rate-all")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("CEMS", "https://cems.test", "secret-key", TimeSpan.FromSeconds(5), 1));
        var sut = new CemsClient(executor, config);

        var result = await sut.GetRateAllAsync(new CemsGetRateAllRequest());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Rates["USD"].Base.Should().Be(1370.5m);
        result.Data.Rates["USD"].Sell.Should().Be(1382.0m);
        result.Data.Rates["USD"].Buy.Should().Be(1361.0m);
        result.Data.Rates["JPY"].Base.Should().Be(9.12m);
        result.Data.Rates["JPY"].Sell.Should().Be(9.2m);
        result.Data.Rates["JPY"].Buy.Should().Be(9.05m);
        result.Data.Rates["INVALID"].Base.Should().BeNull();
        result.Data.Rates["INVALID"].Sell.Should().BeNull();
        result.Data.Rates["INVALID"].Buy.Should().BeNull();
        executor.LastRequestUri!.ToString().Should().Be("https://cems.test/api/cmdV2.php?cmd=C011&key=secret-key");
    }

    [Fact]
    public async Task RegisterTransactionAsync_BuildsTransactionQueryAndParsesResult()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"result":true,"ecode":"OK"}""",
                null,
                "corr-cems-register")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("CEMS", "https://cems.test", "secret-key", TimeSpan.FromSeconds(5), 1));
        var sut = new CemsClient(executor, config);
        var transaction = CreateTransaction();

        var result = await sut.RegisterTransactionAsync(new CemsRegisterTransactionRequest(transaction));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Result.Should().BeTrue();
        result.Data.ErrorCode.Should().Be("OK");
        var uri = executor.LastRequestUri!.ToString();
        uri.Should().Contain("cmd=C030");
        uri.Should().Contain("unique_key=TX001");
        uri.Should().Contain("currency_code=USD");
        uri.Should().Contain("input_money=100000");
        uri.Should().Contain("output_money=72.5");
    }

    private static TransactionModelV2 CreateTransaction()
    {
        return new TransactionModelV2
        {
            TransactionDate = new DateTime(2026, 3, 19, 12, 30, 0),
            TransactionID = "TX001",
            TransactionType = "SELL",
            Customer = new CustomerInfo
            {
                IdType = "1",
                CustomerName = "Kim",
                CustomerNumber = "P123456",
                CustomerNationality = "KR"
            },
            CurrencyPair = new CurrencyPair("USD", 1380m),
            SourceDepositedTotal = 100000m,
            TargetComputedAmount = 72.5m,
            SourceChangeAmount = 500m,
            TargetFailedTotalAmount = 0m,
            ChangeFailedTotalAmount = 0m
        };
    }
}
