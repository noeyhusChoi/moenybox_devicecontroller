using KIOSK.Infrastructure.Hosting;
using KIOSK.ViewModels;
using KIOSK.Presentation.Navigation.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace KIOSK;

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

        var mainWindow = _bootstrapper._serviceProvider.GetRequiredService<MainWindowView>();
        var mainWindowViewModel = _bootstrapper._serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;

        var navigation = _bootstrapper._serviceProvider.GetRequiredService<INavigationService>();
        navigation.SetRootWindow(mainWindow);

        Current.MainWindow = mainWindow;
        mainWindow.Show();
    }
}
