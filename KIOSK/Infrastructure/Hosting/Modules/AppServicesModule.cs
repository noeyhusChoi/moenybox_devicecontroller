using KIOSK.Application.Services;
using KIOSK.Application.Services.DataBase;
using KIOSK.Application.Services.Devices;
using KIOSK.Application.Services.Health;
using KIOSK.Application.Services.Localization;
using KIOSK.Application.Services.Transactions;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Common.Utils;
using KIOSK.Infrastructure.Logging;
using Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class AppServicesModule
    {
        public static IServiceCollection AddAppServicesModule(this IServiceCollection services)
        {
            services.AddSingleton<WithdrawalCassetteService>();
            services.AddSingleton<WithdrawalCassetteServiceV2>();

            services.AddSingleton<IDeviceCatalogService, DeviceCatalogService>();
            services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
            services.AddSingleton<IDeviceCommandCatalogService, DeviceCommandCatalogService>();
            services.AddSingleton<IDatabaseHealthService, DatabaseHealthService>();
            services.AddSingleton<ILocaleInfoProvider, LocaleInfoProvider>();
            services.AddSingleton<ITransactionOutboxService, TransactionOutboxService>();

            services.AddSingleton<ReceiptPrintService>();
            services.AddSingleton<TransactionModelV2>();
            services.AddSingleton<ITransactionServiceV2, TransactionServiceV2>();

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

            return services;
        }
    }
}
