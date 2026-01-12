using KIOSK.Infrastructure.Media;
using KIOSK.Infrastructure.UI;
using KIOSK.Infrastructure.UI.Navigation;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class UiModule
    {
        public static IServiceCollection AddUiModule(this IServiceCollection services)
        {
            services.AddSingleton<NavigationState>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPopupService, PopupService>();
            services.AddScoped<IVideoPlayService, VideoPlayService>();
            services.AddSingleton<IQrGenerateService, QrGenerateService>();
            services.AddSingleton<IInactivityService, InactivityService>();

            return services;
        }
    }
}
