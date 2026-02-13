using KIOSK.Application.Services;
using KIOSK.Application.Services.DataBase;
using KIOSK.Application.Services.Devices;
using KIOSK.Application.Services.Exchange;
using KIOSK.Application.Services.Health;
using KIOSK.Application.Services.Localization;
using KIOSK.Application.Services.Transactions;
using KIOSK.Application.Abstractions;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Common.Utils;
using KIOSK.Infrastructure.Logging;
using KIOSK.Presentation.Features.Exchange.Resources;
using Localization.Resx;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Application.Features.ExchangeV2.UseCases;
using KIOSK.Application.Features.ExchangeV2.Orchestration;
using KIOSK.Infrastructure.Devices.Control;
using KIOSK.Infrastructure.Devices.Status;
using KIOSK.Infrastructure.Devices.Adapters;

namespace KIOSK.CompositionRoot.Modules
{
    public static class AppServicesModule
    {
        public static IServiceCollection AddAppServicesModule(this IServiceCollection services)
        {
            services.AddSingleton<WithdrawalCassetteService>();
            services.AddSingleton<WithdrawalCassetteServiceV2>();

            services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
            services.AddSingleton<IDeviceCommandCatalogService, DeviceCommandCatalogService>();
            services.AddSingleton<IDeviceCommandService, DeviceCommandService>();
            services.AddSingleton<IDepositDeviceService, DepositDeviceService>();
            services.AddSingleton<IIdScannerDeviceService, IdScannerDeviceService>();
            services.AddSingleton<IWithdrawalDeviceService, WithdrawalDeviceService>();
            services.AddSingleton<IPrinterDeviceService, PrinterDeviceService>();
            services.AddSingleton<IQrScannerDeviceService, QrScannerDeviceService>();
            services.AddSingleton<IDepositDevice, DepositDevice>();
            services.AddSingleton<IIdScannerDevice, IdScannerDevice>();
            services.AddSingleton<IDatabaseHealthService, DatabaseHealthService>();
            services.AddSingleton<ILocaleInfoProvider, LocaleInfoProvider>();
            services.AddSingleton<ITransactionOutboxService, TransactionOutboxService>();

            services.AddSingleton<ReceiptPrintService>();
            services.AddSingleton<TransactionModelV2>();
            services.AddSingleton<ITransactionServiceV2, TransactionServiceV2>();
            services.AddSingleton<ITransactionContext>(sp => sp.GetRequiredService<ITransactionServiceV2>());

            services.AddTransient<IExchangeDepositUseCase, ExchangeDepositUseCase>();
            services.AddTransient<IExchangeIdScanUseCase, ExchangeIdScanUseCase>();
            services.AddTransient<IExchangeIdScanGuideUseCase, ExchangeIdScanGuideUseCase>();
            services.AddTransient<IExchangeWithdrawalUseCase, ExchangeWithdrawalUseCase>();
            services.AddTransient<IExchangeSelectLanguageUseCase, ExchangeSelectLanguageUseCase>();
            services.AddTransient<IExchangeSelectCurrencyUseCase, ExchangeSelectCurrencyUseCase>();
            services.AddTransient<IExchangeResultUseCase, ExchangeResultUseCase>();
            services.AddTransient<IExchangeRateListUseCase, ExchangeRateListUseCase>();
            services.AddTransient<IExchangeDepositSessionService, ExchangeDepositSessionService>();
            services.AddTransient<IExchangeDispenseSessionService, ExchangeDispenseSessionService>();
            services.AddTransient<IExchangeIdScanSessionService, ExchangeIdScanSessionService>();
            services.AddTransient<IExchangeRateProvider, ExchangeRateProvider>();
            services.AddTransient<IExchangePolicyProvider, ExchangePolicyProvider>();
            services.AddTransient<IExchangeResultReporter, ExchangeResultReporter>();
            services.AddTransient<IExchangeReceiptPrinter, ExchangeReceiptPrinter>();
            services.AddTransient<IExchangeResultSender, ExchangeResultSender>();
            services.AddTransient<IExchangeResultOutboxUpdater, ExchangeResultOutboxUpdater>();
            services.AddTransient<IExchangeReceiptLocaleProvider, ExchangeReceiptLocaleProvider>();
            services.AddTransient<IExchangeResultViewDataProvider, ExchangeResultViewDataProvider>();
            services.AddTransient<IExchangeTermsResourceProvider, ExchangeTermsResourceProvider>();
            services.AddTransient<IExchangeIdScanInfoResourceProvider, ExchangeIdScanInfoResourceProvider>();
            services.AddTransient<IExchangeDepositAssetsProvider, ExchangeDepositAssetsProvider>();
            services.AddTransient<IExchangeLoadingVideoProvider, ExchangeLoadingVideoProvider>();

            services.AddSingleton<IExchangeV2LimitProvider, ExchangeV2LimitProvider>();
            services.AddTransient<IExchangeV2IdScanUseCase, ExchangeV2IdScanUseCase>();

            services.AddSingleton<IExchangeV2TransactionContext, ExchangeV2TransactionContext>();
            services.AddSingleton<IExchangeV2FlowPolicyResolver, ExchangeV2FlowPolicyResolver>();
            services.AddSingleton<IExchangeV2FlowPolicyValidator, ExchangeV2FlowPolicyValidator>();
            services.AddTransient<IExchangeV2PolicyUseCase, ExchangeV2PolicyUseCase>();
            services.AddTransient<IExchangeV2ComplianceUseCase, ExchangeV2ComplianceUseCase>();
            services.AddTransient<IExchangeV2DepositUseCase, ExchangeV2DepositUseCase>();
            services.AddTransient<IExchangeV2PayoutUseCase, ExchangeV2PayoutUseCase>();
            services.AddTransient<IExchangeV2Orchestrator, ExchangeV2Orchestrator>();

            services.AddSingleton<GtfTaxRefundModel>();
            services.AddSingleton<IGtfTaxRefundService, GtfTaxRefundService>();

            services.AddSingleton(new LocalizationOptions());
            services.AddSingleton<ILocalizationService>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggingService>();
                var options = Options.Create(sp.GetRequiredService<LocalizationOptions>());
                var initialCulture = CultureInfo.CurrentUICulture;
                return new LocalizationService(logger, options, initialCulture);
            });

            services.AddSingleton(new ResxLocalizationOptions());
            services.AddSingleton<IResxLocalizationService>(sp =>
            {
                var logger = sp.GetRequiredService<ILoggingService>();
                var options = Options.Create(sp.GetRequiredService<ResxLocalizationOptions>());
                var initialCulture = CultureInfo.CurrentUICulture;
                return new ResxLocalizationService(logger, options, initialCulture);
            });

            return services;
        }
    }
}
