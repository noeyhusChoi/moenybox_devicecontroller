using Kiosk.Infrastructure.Database;
using Kiosk.Infrastructure.Database.Ef;
using Kiosk.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

namespace Kiosk.Infrastructure.Hosting.Modules
{
    public static class DatabaseModule
    {
    public static IServiceCollection AddDatabaseModule(this IServiceCollection services)
    {
        services.AddMemoryCache();
        var connectionString = DatabaseConfig.DefaultConnectionString;
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

            services.AddOptions<DeviceCommandLogOptions>().BindConfiguration("DeviceCommandLog");

            services.AddSingleton<NoopDeviceCommandLogSink>();
            services.AddSingleton<BufferedDeviceCommandLogSink>();
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<BufferedDeviceCommandLogSink>());
            services.AddSingleton<IDeviceCommandLogSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<DeviceCommandLogOptions>>().Value;
                if (!options.Enabled)
                    return sp.GetRequiredService<NoopDeviceCommandLogSink>();

                if (string.Equals(options.Mode, "Buffered", StringComparison.OrdinalIgnoreCase))
                    return sp.GetRequiredService<BufferedDeviceCommandLogSink>();

                return sp.GetRequiredService<DeviceCommandLogRepository>();
            });

            return services;
        }
    }
}
