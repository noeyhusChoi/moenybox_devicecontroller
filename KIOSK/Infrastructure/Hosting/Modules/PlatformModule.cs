using Kiosk.Infrastructure.Media;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.Infrastructure.Hosting.Modules
{
    public static class PlatformModule
    {
        public static IServiceCollection AddPlatformModule(this IServiceCollection services)
        {
            services.AddSingleton<IAudioPlayService, AudioPlayService>();
            return services;
        }
    }
}
