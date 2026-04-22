using Kiosk.Application.Abstractions;
using Kiosk.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.Infrastructure.Hosting.Modules
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
