using KIOSK.Presentation.Shell.Window.Startup.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class WindowModule
    {
        public static IServiceCollection AddWindowModule(this IServiceCollection services)
        {
            services.AddSingleton<StartupWindowView>();
            services.AddSingleton<MainWindowView>();
            return services;
        }
    }
}
