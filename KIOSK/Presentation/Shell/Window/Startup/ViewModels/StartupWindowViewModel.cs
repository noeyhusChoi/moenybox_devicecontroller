using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Hosting;
using KIOSK.Infrastructure.Initialization;
using KIOSK.Infrastructure.Media;
using KIOSK.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace KIOSK.Presentation.Shell.Window.Startup.ViewModels
{
    public partial class StartupWindowViewModel : ObservableObject
    {
        private readonly IAppInitializer _initializer;
        private readonly IHostController _hostController;
        private readonly IServiceProvider _sp;

        [ObservableProperty] private string message = "초기화 준비 중...";

        private readonly IVideoPlayService _videoPlay;

        private Uri videoPath;

        [ObservableProperty]
        private Brush? backgroundBrush;

        public StartupWindowViewModel(
            IAppInitializer initializer,
            IHostController hostController,
            IServiceProvider sp,
            IVideoPlayService videoPlay)
        {
            _initializer = initializer;
            _hostController = hostController;
            _sp = sp;
            _videoPlay = videoPlay;

            _initializer.ProgressChanged += msg => Message = msg;

            videoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "Loading.mp4"), UriKind.Absolute);
            
            BackgroundBrush = _videoPlay.BackgroundBrush;
            _videoPlay.SetSource(videoPath, loop: true, mute: true, autoPlay: true);
        }

        public async Task RunAsync()
        {
            try
            {
                // 1) 초기화 실행
                await _initializer.InitializeAsync();

                // 2) Host 시작
                await _hostController.StartAsync();

                // 3) MainWindow 전환
                var main = _sp.GetRequiredService<MainWindowView>();
                main.DataContext = _sp.GetRequiredService<MainWindowViewModel>();
                main.Show();

                // 4) 로딩 창 닫기
                System.Windows.Application.Current.Windows
                    .OfType<StartupWindowView>()
                    .First()
                    .Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
