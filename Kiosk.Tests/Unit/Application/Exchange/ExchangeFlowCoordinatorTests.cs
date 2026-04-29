using Kiosk.Application.Features.ExchangeV2.Orchestration;
using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.Application.Features.ExchangeV2.StateMachine;
using Kiosk.Application.Services.Exchange;
using Microsoft.Extensions.Logging;

namespace Kiosk.Tests.Unit.Application.Exchange;

public sealed class ExchangeFlowCoordinatorTests
{
    [Fact]
    public async Task StartAsync_InitializesStartStep()
    {
        var scanSession = Substitute.For<IExchangeScanSession>();
        var depositSession = Substitute.For<IExchangeDepositSession>();
        var withdrawalSession = Substitute.For<IExchangeWithdrawalSession>();
        var cashBalanceProvider = Substitute.For<IExchangeCashBalanceProvider>();
        var depositLimitProvider = Substitute.For<IDepositLimitProvider>();
        var logger = Substitute.For<ILogger<ExchangeFlowCoordinator>>();
        var sut = new ExchangeFlowCoordinator(
            scanSession,
            depositSession,
            withdrawalSession,
            cashBalanceProvider,
            depositLimitProvider,
            logger);

        await sut.StartAsync();

        sut.Context.CurrentStep.Should().Be(ExchangeStep.Start);
        sut.Context.IsTermsAgreed.Should().BeFalse();
    }
}
