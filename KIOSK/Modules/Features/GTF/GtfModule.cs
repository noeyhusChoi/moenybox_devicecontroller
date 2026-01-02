using KIOSK.Modules.GTF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Modules.GTF
{
    public static class GtfModule
    {
        public static IServiceCollection AddGtfModule(this IServiceCollection services)
        {
            // ViewModels
            services.AddTransient<GtfLanguageSelectViewModel>();
            
            services.AddTransient<GtfIdScanConsentViewModel>();
            services.AddTransient<GtfIdScanGuideViewModel>();
            services.AddTransient<GtfIdScanProcessViewModel>();
            services.AddTransient<GtfIdScanCompleteViewModel>();        // not used, ID 스캔 후 바로 환급 수단 선택으로 이동

            services.AddTransient<GtfRefundMethodSelectViewModel>();
            
            services.AddTransient<GtfCreditGuideViewModel>();
            services.AddTransient<GtfCreditRegisterViewModel>();
            
            services.AddTransient<GtfAlipayGuideViewModel>();
            services.AddTransient<GtfAlipayRegisterViewModel>();

            services.AddTransient<GtfWeChatGuideViewModel>();
            services.AddTransient<GtfWeChatRegisterViewModel>();

            services.AddTransient<GtfRefundVoucherRegisterViewModel>();
            services.AddTransient<GtfRefundSignatureViewModel>();

            // Services
            services.AddTransient<Services.GtfTaxRefundService>();

            // StateMachine
            services.AddSingleton<FSM.GtfStateMachine>();

            // Factories
            // services.AddTransient<Factories.RefundMethodGuideFactory>();

            return services;
        }
    }
}
