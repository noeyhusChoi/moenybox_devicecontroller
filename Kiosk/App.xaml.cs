using Kiosk.Application.Services.Resx;
using Kiosk.Infrastructure.Hosting;
using Kiosk.Infrastructure.Updates;
using Kiosk.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Velopack;

namespace Kiosk;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private AppBootstrapper? _bootstrapper;

    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        base.OnStartup(e);

        _bootstrapper = new AppBootstrapper();
        var appUpdateService = _bootstrapper._serviceProvider.GetRequiredService<IAppUpdateService>();
        appUpdateService.CheckAndApplyOnStartupAsync().GetAwaiter().GetResult();
        var hostController = _bootstrapper._serviceProvider.GetRequiredService<IHostController>();
        hostController.StartAsync().GetAwaiter().GetResult();

        var resxLocalizationService = _bootstrapper._serviceProvider.GetRequiredService<IResxLocalizationService>();
        ResxLocalizationProvider.Initialize(resxLocalizationService);

        var mainWindow = _bootstrapper._serviceProvider.GetRequiredService<MainWindowView>();
        var mainWindowViewModel = _bootstrapper._serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;

        Current.MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_bootstrapper is not null)
        {
            var hostController = _bootstrapper._serviceProvider.GetService<IHostController>();
            hostController?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _bootstrapper?.Dispose();
        base.OnExit(e);
    }
}
