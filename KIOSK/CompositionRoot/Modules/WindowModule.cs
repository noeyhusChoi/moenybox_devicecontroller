using KIOSK;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.CompositionRoot.Modules
{
    public static class WindowModule
    {
        public static IServiceCollection AddWindowModule(this IServiceCollection services)
        {
            services.AddSingleton<MainWindowView>();
            return services;
        }
    }
}
