using KIOSK.Presentation.Features.Environment.Layout.ViewModels;
using KIOSK.Presentation.Features.Environment.Pages.ViewModels;
using KIOSK.Presentation.Features.Exchange.Layout.ViewModels;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels;
using KIOSK.Presentation.Features.ExchangeV2.Layout.ViewModels;
using KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels;
using KIOSK.Presentation.Features.ExchangeV2.Popup.ViewModels;
using KIOSK.Presentation.Features.ExchangeV2.Popup.Views;
using KIOSK.Presentation.Features.GTF.Layout.ViewModels;
using KIOSK.Presentation.Features.GTF.Pages.ViewModels;
using KIOSK.Presentation.Features.GTF.Popup.ViewModels;
using KIOSK.Presentation.Features.Menu.Layout.ViewModels;
using KIOSK.Presentation.Features.Menu.Pages.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Layout.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Pages.ViewModels;
using KIOSK.Presentation.Shell.Top.Main.Pages.ViewModels;
using KIOSK.ViewModels;
using KIOSK.Presentation.Features.Exchange.Popup.ViewModels;
using KIOSK.Presentation.Features.Startup.Layout.ViewModels;
using KIOSK.Presentation.Features.Startup.Pages.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.CompositionRoot.Modules
{
    public static class ViewModelModule
    {
        public static IServiceCollection AddViewModelModule(this IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoadingViewModel>();

            services.AddSingleton<FooterViewModel>();

            services.AddScoped<EnvironmentLayoutViewModel>();
            services.AddScoped<MenuLayoutViewModel>();
            services.AddScoped<MenuV2LayoutViewModel>();
            services.AddScoped<ExchangeLayoutViewModel>();
            services.AddScoped<ExchangeV2LayoutViewModel>();
            services.AddScoped<GtfLayoutViewModel>();
            services.AddScoped<StartupLayoutViewModel>();

            services.AddScoped<EnvironmentViewModel>();
            services.AddTransient<EnvironmentCassetteSettingViewModel>();
            services.AddScoped<DeviceStatusViewModel>();
            services.AddScoped<ResxLocalizationTestViewModel>();

            services.AddScoped<MenuViewModel>();
            services.AddScoped<MenuV2ViewModel>();
            services.AddScoped<StartupViewModel>();
            services.AddScoped<ExchangeV2FlowHeaderViewModel>();

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

            services.AddTransient<ExchangeV2LanguageSelectViewModel>();
            services.AddTransient<ExchangeV2ExchangeTypeSelectViewModel>();
            services.AddTransient<ExchangeV2ExchangeMethodSelectViewModel>();
            services.AddTransient<ExchangeV2ExchangeCurrencySelectViewModel>();
            services.AddTransient<ExchangeV2IdScanConsentViewModel>();
            services.AddTransient<ExchangeV2IdScanProcessViewModel>();
            services.AddTransient<ExchangeV2IdScanCompleteViewModel>();
            services.AddTransient<ExchangeV2TermsPopupViewModel>();
            services.AddTransient<ExchangeV2IdScanFailedPopupViewModel>();
            // services.AddTransient<ExchangeV2TermsPopupView>();

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
