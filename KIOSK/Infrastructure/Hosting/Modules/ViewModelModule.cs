using KIOSK.Presentation.Features.Environment.Shell.ViewModels;
using KIOSK.Presentation.Features.Environment.ViewModels;
using KIOSK.Presentation.Features.Exchange.Shell.ViewModels;
using KIOSK.Presentation.Features.GTF.Shell.ViewModels;
using KIOSK.Presentation.Features.GTF.ViewModels;
using KIOSK.Presentation.Features.Menu.Shell.ViewModels;
using KIOSK.Presentation.Shell.Top.Admin.ViewModels;
using KIOSK.Presentation.Shell.Top.Main.ViewModels;
using KIOSK.Presentation.Shell.Window.Startup.ViewModels;
using KIOSK.ViewModels;
using KIOSK.ViewModels.Exchange.Popup;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class ViewModelModule
    {
        public static IServiceCollection AddViewModelModule(this IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoadingViewModel>();
            services.AddSingleton<StartupWindowViewModel>();

            services.AddSingleton<AdminShellViewModel>();
            services.AddSingleton<UserShellViewModel>();
            services.AddSingleton<FooterViewModel>();

            services.AddScoped<EnvironmentShellViewModel>();
            services.AddScoped<MenuSubShellViewModel>();
            services.AddScoped<ExchangeShellViewModel>();
            services.AddScoped<GtfSubShellViewModel>();

            services.AddScoped<EnvironmentViewModel>();
            services.AddTransient<EnvironmentCassetteSettingViewModel>();
            services.AddScoped<DeviceStatusViewModel>();

            services.AddScoped<MenuViewModel>();

            services.AddTransient<ExchangeLanguageViewModel>();
            services.AddTransient<ExchangeCurrencyViewModel>();
            services.AddTransient<ExchangeIDScanConsentViewModel>();
            services.AddTransient<ExchangeIDScanGuideViewModel>();
            services.AddTransient<ExchangeIDScanProcessViewModel>();
            services.AddTransient<ExchangeIDScanCompleteViewModel>();
            services.AddTransient<ExchangeDepositViewModel>();
            services.AddTransient<ExchangeWithdrawalViewModel>();
            services.AddTransient<ExchangeResultViewModel>();
            services.AddTransient<ExchangeCompleteViewModel>();
            services.AddTransient<ExchangePopupTermsViewModel>();
            services.AddTransient<ExchangePopupIDScanInfoViewModel>();

            services.AddTransient<GtfLanguageSelectViewModel>();
            services.AddTransient<GtfIdScanConsentViewModel>();
            services.AddTransient<GtfIdScanGuideViewModel>();
            services.AddTransient<GtfIdScanProcessViewModel>();
            services.AddTransient<GtfIdScanCompleteViewModel>();
            services.AddTransient<GtfRefundMethodSelectViewModel>();
            services.AddTransient<GtfCreditGuideViewModel>();
            services.AddTransient<GtfAlipayGuideViewModel>();
            services.AddTransient<GtfWeChatGuideViewModel>();
            services.AddTransient<GtfRefundVoucherRegisterViewModel>();
            services.AddTransient<GtfRefundSignatureViewModel>();
            services.AddTransient<GtfCreditRegisterViewModel>();
            services.AddTransient<GtfAlipayRegisterViewModel>();
            services.AddTransient<GtfWeChatRegisterViewModel>();
            services.AddTransient<GtfAlipayAccountSelectViewModel>();
            services.AddTransient<GtfWeChatRegisterGuideViewModel>();
            services.AddTransient<GtfRefundCompleteViewModel>();
            services.AddTransient<GtfTestPopupViewModel>();

            return services;
        }
    }
}
