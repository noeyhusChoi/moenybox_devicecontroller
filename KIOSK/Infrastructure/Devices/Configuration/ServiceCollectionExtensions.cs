using KIOSK.Device.Core;
using KIOSK.Device.Drivers.Deposit;
using KIOSK.Device.Drivers.E200Z;
using KIOSK.Device.Drivers.IdScanner;
using KIOSK.Device.Drivers.Printer;
using KIOSK.Devices.Drivers.HCDM;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Management;
using KIOSK.Infrastructure.Database;
using KIOSK.Infrastructure.Database.Repositories;
using KIOSK.Status;
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
        services.AddSingleton<IStatusNotifier, AggregatingStatusNotifier>();
        services.AddSingleton<IStatusRepository, NullStatusRepository>();
        services.AddSingleton<IErrorPolicy, StandardErrorPolicy>();
        services.AddSingleton<IErrorMessageProvider, StandardErrorMessageProvider>();
        services.AddSingleton<DeviceCommandLogRepository>();
        services.AddSingleton<IDeviceHost, DeviceHost>();
        services.AddSingleton<IDeviceCommandCatalog, DeviceCommandCatalog>();
        services.AddSingleton<IStatusPipeline, StatusPipeline>();
        services.AddSingleton<ICommandProvider, E200ZCommandProvider>();
        services.AddSingleton<ICommandProvider, PrinterCommandProvider>();
        services.AddSingleton<ICommandProvider, Hcdm10kCommandProvider>();
        services.AddSingleton<ICommandProvider, DepositCommandProvider>();
        services.AddSingleton<ICommandProvider, IdScannerCommandProvider>();
        services.AddSingleton<IDeviceManager, DeviceService>();

        services.AddHostedService<DeviceBootstrapper>();

        return services;
    }
}
