// AppBootstrapper.cs (refactored)
using KIOSK.Composition.Modules;
using KIOSK.Infrastructure.Initialization;
using KIOSK.Infrastructure.Logging;
using KIOSK.Services;
using KIOSK.Shell.Window.Startup.ViewModels;
using KIOSK.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KIOSK.Composition;

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
                services.AddAppModules();

                // HostedService 등록
                services.AddHostedService<BackgroundTaskService>();

                // StartupWindow
                services.AddSingleton<StartupWindowView>();
                services.AddSingleton<StartupWindowViewModel>();

                // MainWindow
                services.AddSingleton<MainWindowView>();
                services.AddSingleton<MainWindowViewModel>();
            })
            .ConfigureLogging((ctx, logging) =>
            {
                logging.ClearProviders();
            })
            .Build();
    }

    public void Dispose() => _host.Dispose();
}
