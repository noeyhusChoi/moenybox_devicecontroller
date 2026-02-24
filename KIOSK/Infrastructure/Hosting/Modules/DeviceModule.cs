using KIOSK.Application.Services;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime.Factories;
using KIOSK.Infrastructure.Devices.Runtime;
using KIOSK.Infrastructure.Devices.Status;
using KIOSK.Infrastructure.Devices.Adapters;
using KIOSK.Infrastructure.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using KIOSK.Infrastructure.Database.Models;
using KIOSK.Infrastructure.Health;
using KIOSK.DeviceCommon.Devices;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class DeviceModule
    {
        public static IServiceCollection AddDeviceModule(this IServiceCollection services)
        {
            services.AddSingleton<ITransportFactory, TransportFactory>();
            services.AddSingleton<IDeviceDriverFactory, DeviceDriverFactory>();

            services.AddSingleton<IStatusStore, StatusStore>();
            services.AddSingleton<IStatusNotifyService, StatusNotifyService>();
            services.AddSingleton<DeviceStatusLogRepository>();
            services.AddSingleton<IStatusLogService, StatusLogService>();
            services.AddSingleton<IErrorPolicy, StandardErrorPolicy>();
            services.AddSingleton<IErrorMessageProvider, StandardErrorMessageProvider>();
            services.AddSingleton<IHealthPipeline, HealthPipeline>();
            services.AddHostedService<NetworkHealthSupervisorHostedService>();
            services.AddHostedService<DiskHealthSupervisorHostedService>();
            services.AddSingleton<IDeviceCommandCatalog, DeviceCommandCatalog>();
            services.AddSingleton<IStatusPipeline, StatusPipeline>();
            services.AddSingleton<IDeviceManager, DeviceManager>();
            services.AddSingleton<IDeviceRuntimePort, DeviceRuntimeAdapter>();
            services.AddSingleton<DeviceErrorEventService>();
            services.AddSingleton<ExchangeRateModel>();

            return services;
        }
    }
}
