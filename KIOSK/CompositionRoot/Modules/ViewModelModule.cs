using KIOSK.Presentation.Features.Environment.Shell.ViewModels;
using KIOSK.Presentation.Features.Environment.Pages.ViewModels;
using KIOSK.Presentation.Features.Exchange.Shell.ViewModels;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels;
using KIOSK.Presentation.Features.ExchangeV2.Shell.ViewModels;
using KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels;
using KIOSK.Presentation.Features.GTF.Shell.ViewModels;
using KIOSK.Presentation.Features.GTF.Pages.ViewModels;
using KIOSK.Presentation.Features.GTF.Pages.ViewModels.Popup;
using KIOSK.Presentation.Features.Menu.Shell.ViewModels;
using KIOSK.Presentation.Features.Menu.Pages.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Shell.ViewModels;
using KIOSK.Presentation.Features.MenuV2.Pages.ViewModels;
using KIOSK.Presentation.Shared.Flow.ViewModels;
using KIOSK.Presentation.Shell.Top.Main.Pages.ViewModels;
using KIOSK.Presentation.Shell.Window.Startup.ViewModels;
using KIOSK.ViewModels;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels.Popup;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.CompositionRoot.Modules
{
    public static class ViewModelModule
    {
        public static IServiceCollection AddViewModelModule(this IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoadingViewModel>();
            services.AddSingleton<StartupWindowViewModel>();

            services.AddSingleton<FooterViewModel>();

            services.AddScoped<EnvironmentShellViewModel>();
            services.AddScoped<MenuShellViewModel>();
            services.AddScoped<MenuV2ShellViewModel>();
            services.AddScoped<ExchangeShellViewModel>();
            services.AddScoped<ExchangeV2ShellViewModel>();
            services.AddScoped<GtfShellViewModel>();

            services.AddScoped<EnvironmentViewModel>();
            services.AddTransient<EnvironmentCassetteSettingViewModel>();
            services.AddScoped<DeviceStatusViewModel>();
            services.AddScoped<ResxLocalizationTestViewModel>();

            services.AddScoped<MenuViewModel>();
            services.AddScoped<MenuV2ViewModel>();
            services.AddSingleton<FlowProgressViewModel>();
            services.AddSingleton<IFlowDefinitionProvider, FlowDefinitionProvider>();
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
