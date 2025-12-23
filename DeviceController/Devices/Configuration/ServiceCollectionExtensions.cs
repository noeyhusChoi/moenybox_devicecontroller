using KIOSK.Device.Core;
using KIOSK.Device.Drivers.Deposit;
using KIOSK.Device.Drivers.E200Z;
using KIOSK.Device.Drivers.IdScanner;
using KIOSK.Device.Drivers.Printer;
using KIOSK.Device.Transport;
using KIOSK.Devices.Drivers.HCDM;
using KIOSK.Devices.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIOSK.Devices.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevicePlatform(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DevicePlatformOptions>(config.GetSection(DevicePlatformOptions.SectionName));

        services.AddSingleton<ITransportFactory, TransportFactory>();
        services.AddSingleton<IDeviceFactory, DeviceFactory>();

        services.AddSingleton<IDeviceStatusStore, DeviceStatusStore>();
        services.AddSingleton<IDeviceRuntime, DeviceRuntime>();
        services.AddSingleton<IDeviceCommandCatalog, DeviceCommandCatalog>();
        services.AddSingleton<IDeviceStatusPipeline, DeviceStatusPipeline>();
        services.AddSingleton<IDeviceCommandProvider, E200ZCommandProvider>();
        services.AddSingleton<IDeviceCommandProvider, PrinterCommandProvider>();
        services.AddSingleton<IDeviceCommandProvider, Hcdm10kCommandProvider>();
        services.AddSingleton<IDeviceCommandProvider, DepositCommandProvider>();
        services.AddSingleton<IDeviceCommandProvider, IdScannerCommandProvider>();
        services.AddSingleton<IDeviceManager, DeviceManagerV2>();

        services.AddHostedService<DeviceBootstrapper>();

        return services;
    }
}
