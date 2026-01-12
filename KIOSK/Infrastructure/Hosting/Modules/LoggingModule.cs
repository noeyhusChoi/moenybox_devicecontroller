using KIOSK.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.Hosting.Modules
{
    public static class LoggingModule
    {
        public static IServiceCollection AddLoggingModule(this IServiceCollection services)
        {
            services.AddSingleton<ILoggingService, LoggingService>();
            return services;
        }
    }
}
