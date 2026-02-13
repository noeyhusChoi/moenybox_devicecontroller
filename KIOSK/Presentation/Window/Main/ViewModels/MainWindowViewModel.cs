using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Storage;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using System.Collections.ObjectModel;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.Environment.Pages.ViewModels;
using KIOSK.Presentation.Features.Environment.Layout.ViewModels;
using KIOSK.Presentation.Features.Menu.Layout.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;
using KIOSK.Presentation.Features.MenuV2.Layout.ViewModels;
using KIOSK.Presentation.Features.Startup.Layout.ViewModels;
using KIOSK.Presentation.Window.Services;

namespace KIOSK.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly INavigationService _nav;
        private readonly CemsApiService _cems;

        [ObservableProperty]
        private object currentLayout; // MainWindow의 Layout Content

        public MainWindowViewModel(INavigationService nav, CemsApiService cems)
        {
            _nav = nav;
            CurrentLayout = null;

            _cems = cems;
        }

        public async Task InitializeAsync()
        {
            await _nav.NavigateLayout<StartupLayoutViewModel>();
        }

        [RelayCommand] private void F0() { }

        [RelayCommand]
        private void F1()
        {
            Trace.WriteLine($"LAYOUT     [{_nav.ActiveLayout}]");
            Trace.WriteLine($"PAGE       [{_nav.ActivePage}]");
            Trace.WriteLine($"LAYOUT_POPUP [{_nav.ActiveLayout?.PopupContent}] ");
        }

        [RelayCommand]
        private void F2()
        {
            if (_nav.ActiveLayout is EnvironmentLayoutViewModel)
                _nav.NavigateLayout<MenuV2LayoutViewModel>();
            else
                _nav.NavigateLayout<EnvironmentLayoutViewModel>();
        }

        [RelayCommand] private void F3() { MonitorMover.MoveActiveWindowToNextScreen(); }

        [RelayCommand]
        private async Task F4()
        {
            string ms = "## 본점 / 09 ##\n장치(지폐 방출기) 오류\n" +
                        "[장애] 원화 인출 에러 : 105,000 KRW\n" +
                        "개인정보 : 859609428 / JEEVAN VIJAYAN\n\n" +
                        "지폐 방출기 1:Result Code : 9\n" +
                        "Error Code : 40080\n" +
                        "Error Message : Communication Result : 1번 카세트 픽업 실패(카세트에 매체는 존재하는 상태) \n" +
                        "\nCassette: 0 Exit: 0, Reject: 0" +
                        "\nCassette: 1 Exit: 0, Reject: 0" +
                        "\nCassette: 2 Exit: 0, Reject: 0" +
                        "\nCassette: 3 Exit: 0, Reject: 0" +
                        "\n\n";

            ms = Regex.Replace(ms, @"\r?\n", "\\n");
            var xx = await _cems.SmsAsync(DateTime.Now, "ADM", ms, CancellationToken.None);
        }

        [RelayCommand] private void F5() { }
    }
}
