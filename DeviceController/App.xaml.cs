using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DeviceController.ViewModels;
using KIOSK.Devices.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DeviceController;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddDevicePlatform(ctx.Configuration);

                services.Configure<HostOptions>(o =>
                {
                    // 장치 연결/재시도 중 발생하는 예외로 앱 전체가 내려가는 것을 방지
                    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                });

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync().ConfigureAwait(false);
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            try { await _host.StopAsync().ConfigureAwait(false); } catch { }
            _host.Dispose();
            _host = null;
        }

        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try { Trace.WriteLine($"[UI] Unhandled: {e.Exception}"); } catch { }

        // 장치 포트 제거 등 외부 요인으로 발생한 예외는 앱이 종료되지 않도록 처리.
        e.Handled = e.Exception is
            OperationCanceledException or
            ObjectDisposedException or
            System.IO.IOException or
            UnauthorizedAccessException or
            InvalidOperationException;
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try { Trace.WriteLine($"[AppDomain] Unhandled: {e.ExceptionObject}"); } catch { }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try { Trace.WriteLine($"[Task] Unobserved: {e.Exception}"); } catch { }
        e.SetObserved();
    }
}

