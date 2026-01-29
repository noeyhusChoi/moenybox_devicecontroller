using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Hosting;
using KIOSK.Infrastructure.Initialization;
using KIOSK.Presentation.Features.MenuV2.Shell.ViewModels;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Shared.Abstractions;
using System.Windows;
using System.Windows.Threading;

namespace KIOSK.Presentation.Features.Startup.Pages.ViewModels
{
    public partial class StartupViewModel : ObservableObject, INavigable
    {
        private readonly IAppInitializer _initializer;
        private readonly IHostController _hostController;
        private readonly INavigationService _navigation;

        private bool _initialized;

        [ObservableProperty]
        private string message = "초기화 준비 중...";

        [ObservableProperty]
        private string? lottieSourcePath;

        public StartupViewModel(
            IAppInitializer initializer,
            IHostController hostController,
            INavigationService navigation)
        {
            _initializer = initializer;
            _hostController = hostController;
            _navigation = navigation;

            _initializer.ProgressChanged += OnProgressChanged;
            LottieSourcePath = @"C:\Users\niaci\Downloads\Shape Animation.json";
        }

        public Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            if (_initialized)
                return Task.CompletedTask;

            _initialized = true;
            _ = RunStartupFlowAsync(ct);
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            _initializer.ProgressChanged -= OnProgressChanged;
            return Task.CompletedTask;
        }

        private void OnProgressChanged(string msg)
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
        }

        private async Task RunStartupFlowAsync(CancellationToken ct)
        {
            try
            {
                await Task.Run(() => _initializer.InitializeAsync(), ct);

                ct.ThrowIfCancellationRequested();

                await _hostController.StartAsync(ct);

                if (!ct.IsCancellationRequested)
                {
                    await _navigation.NavigateLayout<MenuV2ShellViewModel>();
                }
            }
            catch (OperationCanceledException)
            {
                // 앱 종료 시 취소만 전달
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current?.Shutdown();
            }
        }
    }
}
