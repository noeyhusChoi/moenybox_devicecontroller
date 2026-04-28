using Kiosk.Infrastructure.Hosting.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.CompositionRoot.Modules
{
    public static class BootstrapExtensions
    {
        public static IServiceCollection AddAppModules(this IServiceCollection services)
        {
            services.AddLoggingModule();
            services.AddDatabaseModule();
            services.AddIntegrationModule();
            services.AddPlatformModule();
            services.AddOcrModule();
            services.AddUpdateModule();
            services.AddAppServicesModule();
            services.AddViewModelModule();
            services.AddHostModule();
            services.AddWindowModule();
            return services;
        }
    }
}
