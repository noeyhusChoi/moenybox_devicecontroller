using Kiosk.Infrastructure.Integrations.Cems;
using Kiosk.Infrastructure.Integrations.Common;
using Kiosk.Infrastructure.Integrations.Gtf;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.Infrastructure.Hosting.Modules;

public static class IntegrationModule
{
    public static IServiceCollection AddIntegrationModule(this IServiceCollection services)
    {
        services.AddHttpClient("ExternalApi");
        services.AddSingleton<IHttpExecutor, HttpExecutor>();
        services.AddSingleton<IProviderConfigResolver, ProviderConfigResolver>();
        services.AddScoped<ICemsClient, CemsClient>();
        services.AddScoped<IGtfClient, GtfClient>();
        return services;
    }
}
