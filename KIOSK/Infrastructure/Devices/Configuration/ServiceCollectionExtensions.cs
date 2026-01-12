using KIOSK.Device.Core;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Infrastructure.Database;
using KIOSK.Infrastructure.Database.Repositories;
using KIOSK.Infrastructure.Management.Status;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIOSK.Devices.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevicePlatform(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DevicePlatformOptions>(config.GetSection(DevicePlatformOptions.SectionName));

        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ITransportFactory, TransportFactory>();
        services.AddSingleton<IDeviceFactory, DeviceFactory>();

        services.AddSingleton<IStatusStore, StatusStore>();
        services.AddSingleton<IStatusNotifyService, StatusNotifyService>();
        services.AddSingleton<DeviceStatusLogRepository>();
        services.AddSingleton<IStatusLogService, StatusLogService>();
        services.AddSingleton<IErrorPolicy, StandardErrorPolicy>();
        services.AddSingleton<IErrorMessageProvider, StandardErrorMessageProvider>();
        services.AddSingleton<DeviceCommandLogRepository>();
        services.AddSingleton<IDeviceHost, DeviceHost>();
        services.AddSingleton<IDeviceCommandCatalog, DeviceCommandCatalog>();
        services.AddSingleton<IStatusPipeline, StatusPipeline>();
        services.AddSingleton<IDeviceManager, DeviceService>();

        services.AddHostedService<DeviceBootstrapper>();

        return services;
    }
}
