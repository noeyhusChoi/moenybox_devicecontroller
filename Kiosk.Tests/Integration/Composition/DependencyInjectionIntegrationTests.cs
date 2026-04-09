using Kiosk.Application.Contracts;
using Kiosk.CompositionRoot.Modules;
using Kiosk.Infrastructure.Integrations.Cems;
using Kiosk.Infrastructure.Integrations.Gtf;
using Kiosk.Infrastructure.Hosting.Modules;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kiosk.Tests.Integration.Composition;

public sealed class DependencyInjectionIntegrationTests
{
    [Fact]
    public void Modules_RegisterExchangeAndTaxRefundServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddIntegrationModule();
        services.AddAppServicesModule();

        using var provider = services.BuildServiceProvider();

        var exchangeService = provider.GetRequiredService<IExchangeService>();
        var taxRefundService = provider.GetRequiredService<ITaxRefundService>();
        var cemsClient = provider.GetRequiredService<ICemsClient>();
        var gtfClient = provider.GetRequiredService<IGtfClient>();

        exchangeService.Should().NotBeNull();
        taxRefundService.Should().NotBeNull();
        cemsClient.Should().NotBeNull();
        gtfClient.Should().NotBeNull();
    }
}
