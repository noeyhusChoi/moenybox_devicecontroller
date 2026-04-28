using Kiosk.Application.Services.Resx;
using Kiosk.Infrastructure.Hosting;
using Kiosk.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Kiosk;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private AppBootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        base.OnStartup(e);

        _bootstrapper = new AppBootstrapper();

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
        _bootstrapper?.Dispose();
        base.OnExit(e);
    }
}
