using KIOSK.Infrastructure.Hosting;
using KIOSK.Infrastructure.Initialization;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
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
