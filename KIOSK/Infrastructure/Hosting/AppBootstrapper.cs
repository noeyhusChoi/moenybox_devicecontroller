// AppBootstrapper.cs (refactored)
using KIOSK.Infrastructure.Hosting.Modules;
using KIOSK.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KIOSK.Infrastructure.Hosting;

// TODO: Bootstrap 코드 정리 ( 레이어별 분류 )
public class AppBootstrapper : IDisposable
{
    private readonly IHost _host;

    public IServiceProvider _serviceProvider => _host.Services;
    public AppBootstrapper()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                var loggingService = new LoggingService();
                services.AddSingleton<ILoggingService>(loggingService);
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog(Log.Logger, dispose: false);
                });

                services.AddAppModules();
            })
            .Build();
    }

    public void Dispose() => _host.Dispose();
}
