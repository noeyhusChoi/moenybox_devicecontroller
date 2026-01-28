using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Hosting;
using KIOSK.Infrastructure.Initialization;
using KIOSK.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using KIOSK.Presentation.Shell.Window.Startup.Views;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Shell.Window.Startup.ViewModels
{
    public partial class StartupWindowViewModel : ObservableObject
    {
        private readonly IAppInitializer _initializer;
        private readonly IHostController _hostController;
        private readonly IServiceProvider _sp;
        private readonly INavigationService _nav;

        [ObservableProperty] private string message = "초기화 준비 중...";

        [ObservableProperty]
        private string? lottieSourcePath;

        public StartupWindowViewModel(
            IAppInitializer initializer,
            IHostController hostController,
            IServiceProvider sp,
            INavigationService nav)
        {
            _initializer = initializer;
            _hostController = hostController;
            _sp = sp;
            _nav = nav;

            _initializer.ProgressChanged += msg =>
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher is null)
                {
                    Message = msg;
                    return;
                }

                if (dispatcher.CheckAccess())
                {
                    Message = msg;
                }
                else
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Message = msg));
                }
            };

            LottieSourcePath = @"C:\Users\niaci\Downloads\Shape Animation.json";
        }

        public async Task RunAsync()
        {
            try
            {
                // 1) 초기화 실행
                await Task.Run(() => _initializer.InitializeAsync());

                // 2) Host 시작
                await _hostController.StartAsync();

                // 3) MainWindow 전환
                var main = _sp.GetRequiredService<MainWindowView>();
                main.DataContext = _sp.GetRequiredService<MainWindowViewModel>();
                _nav.SetRootHost((IWindow)main);
                System.Windows.Application.Current.MainWindow = main;
                main.Show();

                // 4) 로딩 창 닫기
                var startup = System.Windows.Application.Current.Windows
                    .OfType<StartupWindowView>()
                    .FirstOrDefault();
                startup?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
