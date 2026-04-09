using Kiosk.Application.Models.Exchange;
using Kiosk.Application.Services.Exchange;
using Kiosk.Infrastructure.Integrations.Cems;
using Kiosk.Infrastructure.Integrations.Cems.Models;
using Kiosk.Infrastructure.Integrations.Cems.Requests;
using Kiosk.Infrastructure.Integrations.Cems.Responses;
using Kiosk.Domain.Entities;

namespace Kiosk.Tests.Unit.Application.Exchange;

public sealed class ExchangeServiceTests
{
    [Fact]
    public async Task GetRateAsync_MapsClientResponseToOperationResult()
    {
        var client = Substitute.For<ICemsClient>();
        client.GetRateAsync(Arg.Any<CemsGetRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CemsCommandResult<CemsGetRateResponse>(
                true,
                new CemsGetRateResponse(true, null, "USD", 1320.5m, new Dictionary<string, string?>()),
                null,
                200,
                """{"result":true}""",
                "corr-exchange-service")));
        var sut = new ExchangeService(client);

        var result = await sut.GetRateAsync("USD");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CurrencyCode.Should().Be("USD");
        result.Data.Rate.Should().Be(1320.5m);
        result.Provider.Should().Be("CEMS");
        result.Error.Should().BeNull();
        await client.Received(1).GetRateAsync(
            Arg.Is<CemsGetRateRequest>(x => x.CurrencyCode == "USD"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRateAsync_MapsClientFailureToAppError()
    {
        var client = Substitute.For<ICemsClient>();
        client.GetRateAsync(Arg.Any<CemsGetRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CemsCommandResult<CemsGetRateResponse>(
                false,
                null,
                new CemsError("CEMS_DOWN", "service unavailable", true),
                503,
                string.Empty,
                "corr-exchange-error")));
        var sut = new ExchangeService(client);

        var result = await sut.GetRateAsync("USD");

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("CEMS_DOWN");
        result.Error.Provider.Should().Be("CEMS");
        result.Error.Retryable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckLimitAsync_ComputesRemainingAmount()
    {
        var client = Substitute.For<ICemsClient>();
        client.CheckLimitAsync(Arg.Any<CemsCheckLimitRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CemsCommandResult<CemsCheckLimitResponse>(
                true,
                new CemsCheckLimitResponse(true, null, 2000m, 350m, new Dictionary<string, string?>()),
                null,
                200,
                """{"result":true}""",
                "corr-check-limit")));
        var sut = new ExchangeService(client);

        var result = await sut.CheckLimitAsync("P123456");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsAllowed.Should().BeTrue();
        result.Data.RemainingAmount.Should().Be(1650m);
        result.Data.Provider.Should().Be("CEMS");
        await client.Received(1).CheckLimitAsync(
            Arg.Is<CemsCheckLimitRequest>(x => x.CustomerNumber == "P123456"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRateAllAsync_MapsRateDictionary()
    {
        var client = Substitute.For<ICemsClient>();
        client.GetRateAllAsync(Arg.Any<CemsGetRateAllRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CemsCommandResult<CemsGetRateAllResponse>(
                true,
                new CemsGetRateAllResponse(true, null, new Dictionary<string, Kiosk.Infrastructure.Integrations.Cems.Responses.CurrencyRate>
                {
                    ["USD"] = new(1370.5m, 1382.0m, 1361.0m),
                    ["JPY"] = new(9.12m, 9.2m, 9.05m)
                }, new Dictionary<string, string?>()),
                null,
                200,
                """{"result":true}""",
                "corr-get-rate-all")));
        var sut = new ExchangeService(client);

        var result = await sut.GetRateAllAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Rates["USD"].Base.Should().Be(1370.5m);
        result.Data.Rates["USD"].Sell.Should().Be(1382.0m);
        result.Data.Rates["USD"].Buy.Should().Be(1361.0m);
        result.Data.Rates["JPY"].Base.Should().Be(9.12m);
        result.Provider.Should().Be("CEMS");
        await client.Received(1).GetRateAllAsync(Arg.Any<CemsGetRateAllRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTransactionAsync_MapsRegistrationResult()
    {
        var client = Substitute.For<ICemsClient>();
        client.RegisterTransactionAsync(Arg.Any<CemsRegisterTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CemsCommandResult<CemsRegisterTransactionResponse>(
                true,
                new CemsRegisterTransactionResponse(true, "OK", new Dictionary<string, string?>()),
                null,
                200,
                """{"result":true}""",
                "corr-register-transaction")));
        var sut = new ExchangeService(client);

        var result = await sut.RegisterTransactionAsync(new ExchangeTransactionRegistrationCommand(CreateTransaction()));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Registered.Should().BeTrue();
        result.Data.ReasonCode.Should().Be("OK");
        result.Provider.Should().Be("CEMS");
        await client.Received(1).RegisterTransactionAsync(
            Arg.Is<CemsRegisterTransactionRequest>(x => x.Transaction.TransactionID == "TX001"),
            Arg.Any<CancellationToken>());
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
            SourceChangeAmount = 500m
        };
    }
}
