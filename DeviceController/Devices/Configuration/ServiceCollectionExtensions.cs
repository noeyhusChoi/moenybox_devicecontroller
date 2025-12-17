using KIOSK.Device.Core;
using KIOSK.Device.Transport;
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
        services.AddSingleton<IDeviceCommandBus, DeviceCommandBus>();
        services.AddSingleton<IDeviceCommandCatalog, DeviceCommandCatalog>();
        services.AddSingleton<DeviceErrorEventService>();
        services.AddSingleton<IDeviceManager, DeviceManagerV2>();

        services.AddHostedService<DeviceBootstrapper>();

        return services;
    }
}

