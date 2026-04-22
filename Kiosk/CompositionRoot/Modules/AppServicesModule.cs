using Kiosk.Application.Contracts;
using Kiosk.Application.Features.ExchangeV2.Orchestration;
using Kiosk.Application.Features.ExchangeV2.Services;
using Kiosk.Application.Services.Exchange;
using Kiosk.Application.Services;
using Kiosk.Application.Services.DataBase;
using Kiosk.Application.Services.Devices;
using Kiosk.Application.Services.Devices.Deposit;
using Kiosk.Application.Services.Devices.IdScanner;
using Kiosk.Application.Services.Devices.Withdrawal;
using Kiosk.Application.Services.Resx;
using Kiosk.Application.Services.Theme;
using Kiosk.Application.Services.TaxRefund;
using Kiosk.Application.Services.Time;
using Kiosk.Application.Services.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.CompositionRoot.Modules
{
    public static class AppServicesModule
    {
        public static IServiceCollection AddAppServicesModule(this IServiceCollection services)
        {
            services.AddSingleton<IDeviceRuntimeService, DeviceRuntimeService>();
            services.AddSingleton<IDepositService, DepositService>();
            services.AddSingleton<IIdScannerService, IdScannerService>();
            services.AddSingleton<IWithdrawalService, WithdrawalService>();
            services.AddSingleton<IExchangeScanSession, ExchangeScanSession>();
            services.AddSingleton<IExchangeDepositSession, ExchangeDepositSession>();
            services.AddSingleton<IExchangeWithdrawalSession, ExchangeWithdrawalSession>();
            services.AddSingleton<IExchangeFlowCoordinator, ExchangeFlowCoordinator>();
            services.AddSingleton<IClockService, ClockService>();
            services.AddSingleton<IAppCulture, AppCulture>();
            services.AddSingleton<IResxLocalizationService, ResxLocalizationService>();
            services.AddSingleton<IAppTheme, AppTheme>();
            services.AddSingleton<IExchangeCashBalanceProvider, FixedExchangeCashBalanceProvider>();
            services.AddSingleton<IDepositLimitProvider, FixedDepositLimitProvider>();
            services.AddSingleton<WithdrawalCassetteService>();
            services.AddSingleton<WithdrawalCassetteServiceV2>();
            services.AddSingleton<ITransactionOutboxService, TransactionOutboxService>();
            services.AddScoped<IExchangeService, ExchangeService>();
            services.AddScoped<ITaxRefundService, TaxRefundService>();

            return services;
        }
    }
}
