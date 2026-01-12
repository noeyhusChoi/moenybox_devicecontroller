using KIOSK.Infrastructure.Media;
using KIOSK.Infrastructure.Network;
using KIOSK.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class PlatformModule
    {
        public static IServiceCollection AddPlatformModule(this IServiceCollection services)
        {
            services.AddSingleton<IAudioPlayService, AudioPlayService>();
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<INetworkService, NetworkService>();
            return services;
        }
    }
}
