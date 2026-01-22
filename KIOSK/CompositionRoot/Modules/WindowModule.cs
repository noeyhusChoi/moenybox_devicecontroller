using KIOSK.Presentation.Shell.Window.Startup.ViewModels;
using KIOSK.Presentation.Shell.Window.Startup.Views;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.CompositionRoot.Modules
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
