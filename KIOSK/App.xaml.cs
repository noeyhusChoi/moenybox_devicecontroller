using KIOSK.Infrastructure.Hosting;
using KIOSK.Application.Services;
using KIOSK.Presentation.Shell.Top.Main.ViewModels;
using KIOSK.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using KIOSK.Presentation.Shell.Window.Startup.ViewModels;

namespace KIOSK;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private AppBootstrapper? _bootstrapper;

    protected override async void OnStartup(StartupEventArgs e)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        base.OnStartup(e);
 
        _bootstrapper = new AppBootstrapper();

        // 로딩 화면 준비
        var loadingView = _bootstrapper._serviceProvider.GetRequiredService<StartupWindowView>();
        var loadingVM = _bootstrapper._serviceProvider.GetRequiredService<StartupWindowViewModel>();
        loadingView.DataContext = loadingVM;

        loadingView.Show();

        // 비동기 초기화 시작
        _ = loadingVM.RunAsync();

        //try
        //{
        //    await _bootstrapper.StartAsync();
        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(ex.ToString(), "Startup error");
        //    Trace.WriteLine(ex);
        //    Current.Shutdown();
        //}
    }
}
