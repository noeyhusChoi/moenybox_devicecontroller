using Kiosk.Infrastructure.Hosting;
using Kiosk.Infrastructure.Initialization;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.Infrastructure.Hosting.Modules
{
    public static class HostModule
    {
        public static IServiceCollection AddHostModule(this IServiceCollection services)
        {
            services.AddSingleton<IHostController, HostController>();
            services.AddSingleton<IAppInitializer, AppInitializer>();
            return services;
        }
    }
}
