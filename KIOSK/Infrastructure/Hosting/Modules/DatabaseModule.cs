using KIOSK.Infrastructure.Database;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class DatabaseModule
    {
        public static IServiceCollection AddDatabaseModule(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddMemoryCache();
            var connectionString = DatabaseConfig.DefaultConnectionString;
            services.AddDbContext<KioskDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });
            services.AddDbContextFactory<KioskDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            services.AddSingleton<ApiConfigRepository>();
            services.AddSingleton<DepositCurrencyRepository>();
            services.AddSingleton<KioskRepository>();
            services.AddSingleton<DeviceRepository>();
            services.AddSingleton<DeviceCommandLogRepository>();
            services.AddSingleton<ReceiptRepository>();
            services.AddSingleton<LocaleInfoRepository>();
            services.AddSingleton<WithdrawalCassetteRepository>();
            return services;
        }
    }
}
