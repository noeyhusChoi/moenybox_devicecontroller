using KIOSK.Application.Services;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Infrastructure.Management.Status;
using KIOSK.Infrastructure.Database.Repositories;
using KIOSK.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class DeviceModule
    {
        public static IServiceCollection AddDeviceModule(this IServiceCollection services)
        {
            services.AddSingleton<ITransportFactory, TransportFactory>();
            services.AddSingleton<IDeviceFactory, DeviceFactory>();

            services.AddSingleton<IStatusStore, StatusStore>();
            services.AddSingleton<IStatusNotifyService, StatusNotifyService>();
            services.AddSingleton<DeviceStatusLogRepository>();
            services.AddSingleton<IStatusLogService, StatusLogService>();
            services.AddSingleton<IErrorPolicy, StandardErrorPolicy>();
            services.AddSingleton<IErrorMessageProvider, StandardErrorMessageProvider>();
            services.AddSingleton<IDeviceHost, DeviceHost>();
            services.AddSingleton<IDeviceCommandCatalog, DeviceCommandCatalog>();
            services.AddSingleton<IStatusPipeline, StatusPipeline>();
            services.AddSingleton<IDeviceManager, DeviceService>();
            services.AddSingleton<DeviceErrorEventService>();
            services.AddSingleton<ExchangeRateModel>();

            return services;
        }
    }
}
