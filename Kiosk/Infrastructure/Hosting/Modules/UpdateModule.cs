using Kiosk.Infrastructure.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kiosk.Infrastructure.Hosting.Modules;

public static class UpdateModule
{
    public static IServiceCollection AddUpdateModule(this IServiceCollection services)
    {
        services.AddSingleton(_ => VelopackOptions.LoadFromEnvironment());
        services.AddSingleton<IAppUpdateService, AppUpdateService>();
        services.AddSingleton<IHostedService, UpdateBackgroundService>();
        return services;
    }
}
