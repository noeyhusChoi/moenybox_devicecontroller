using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database;
using KIOSK.Infrastructure.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class DatabaseModule
    {
        public static IServiceCollection AddDatabaseModule(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<DatabaseCache>();

            services.AddSingleton<ApiConfigRepository>();
            services.AddSingleton<DepositCurrencyRepository>();
            services.AddSingleton<KioskRepository>();
            services.AddSingleton<DeviceRepository>();
            services.AddSingleton<DeviceCommandLogRepository>();
            services.AddSingleton<ReceiptRepository>();
            services.AddSingleton<LocaleInfoRepository>();
            services.AddSingleton<WithdrawalCassetteRepository>();
            services.AddSingleton<WithdrawalCassetteModel>();

            return services;
        }
    }
}
