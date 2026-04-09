using Kiosk.Application.Services.Exchange;
using Kiosk.Infrastructure.Integrations.Cems;
using Kiosk.Infrastructure.Integrations.Common;
using Kiosk.Tests.Common.Fakes;

namespace Kiosk.Tests.Integration.Application.Exchange;

public sealed class ExchangeServiceIntegrationTests
{
    [Fact]
    public async Task GetRateAsync_ReturnsMappedResult_WhenServiceAndClientAreWiredTogether()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"result":true,"currency":"USD","rate":"1375.25"}""",
                null,
                "corr-int-exchange-rate")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("CEMS", "https://cems.test", "secret-key", TimeSpan.FromSeconds(5), 1));
        var client = new CemsClient(executor, config);
        var sut = new ExchangeService(client);

        var result = await sut.GetRateAsync("USD");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CurrencyCode.Should().Be("USD");
        result.Data.Rate.Should().Be(1375.25m);
        result.Provider.Should().Be("CEMS");
        executor.LastRequestUri!.ToString().Should().Be("https://cems.test/api/cmdV2.php?currency=USD&cmd=C010&key=secret-key");
    }

    [Fact]
    public async Task CheckLimitAsync_ReturnsRemainingAmount_WhenServiceAndClientAreWiredTogether()
    {
        var executor = new FakeHttpExecutor
        {
            NextResult = new HttpExecutionResult(
                true,
                200,
                """{"result":true,"limit_amt":"2000","used_amt":"350"}""",
                null,
                "corr-int-exchange-limit")
        };
        var config = new FakeProviderConfigResolver()
            .Add(new ProviderEndpointConfig("CEMS", "https://cems.test", "secret-key", TimeSpan.FromSeconds(5), 1));
        var client = new CemsClient(executor, config);
        var sut = new ExchangeService(client);

        var result = await sut.CheckLimitAsync("P123456");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsAllowed.Should().BeTrue();
        result.Data.RemainingAmount.Should().Be(1650m);
        result.Provider.Should().Be("CEMS");
        executor.LastRequestUri!.ToString().Should().Be("https://cems.test/api/cmdV2.php?number=P123456&cmd=C020&key=secret-key");
    }
}
