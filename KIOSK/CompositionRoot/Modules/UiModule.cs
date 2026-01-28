using KIOSK.Application.Abstractions;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.CompositionRoot.Modules
{
    public static class UiModule
    {
        public static IServiceCollection AddUiModule(this IServiceCollection services)
        {
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPopupService, PopupService>();
            services.AddSingleton<IQrGenerateService, QrGenerateService>();
            services.AddSingleton<IInactivityService, InactivityService>();

            return services;
        }
    }
}
