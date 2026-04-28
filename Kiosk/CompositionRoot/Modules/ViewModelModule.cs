using Kiosk.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.CompositionRoot.Modules
{
    public static class ViewModelModule
    {
        public static IServiceCollection AddViewModelModule(this IServiceCollection services)
        {
            services.AddSingleton<IExchangeOptionProvider, ExchangeOptionProvider>();
            services.AddSingleton<IExchangeScreenFactory, ExchangeScreenFactory>();
            services.AddSingleton<IHeaderViewModelFactory, HeaderViewModelFactory>();
            services.AddSingleton<HomeShellViewModel>();
            services.AddSingleton<ExchangeShellViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            return services;
        }
    }
}
