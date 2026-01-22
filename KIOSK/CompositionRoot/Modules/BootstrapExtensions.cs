using KIOSK.Infrastructure.Hosting.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.CompositionRoot.Modules
{
    public static class BootstrapExtensions
    {
        public static IServiceCollection AddAppModules(this IServiceCollection services)
        {
            services.AddLoggingModule();
            services.AddDatabaseModule();
            services.AddDeviceModule();
            services.AddApiModule();
            services.AddPlatformModule();
            services.AddOcrModule();
            services.AddUiModule();
            services.AddAppServicesModule();
            services.AddStateMachineModule();
            services.AddViewModelModule();
            services.AddBackgroundModule();
            services.AddHostModule();
            services.AddWindowModule();
            return services;
        }
    }
}
