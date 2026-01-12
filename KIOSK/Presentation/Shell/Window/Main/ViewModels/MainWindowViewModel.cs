using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Storage;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Application.Services.Devices;
using System.Collections.ObjectModel;
using KIOSK.Infrastructure.UI.Navigation;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Presentation.Features.Environment.ViewModels;
using KIOSK.Presentation.Shell.Contracts;
using KIOSK.Presentation.Features.Environment.Shell.ViewModels;
using KIOSK.Presentation.Features.Menu.Shell.ViewModels;
using KIOSK.Presentation.Shell.Top.Admin.ViewModels;
using KIOSK.Presentation.Shell.Top.Main.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KIOSK.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IRootShellHost
    {
        private readonly INavigationService _nav;
        private readonly UserShellViewModel _rootShell;   // TopShell 1
        private readonly IDeviceCatalogService _repo;
        private readonly CemsApiService _cems;

        [ObservableProperty]
        private object rootViewModel; // MainWindow의 Content

        public MainWindowViewModel(INavigationService nav, UserShellViewModel rootShell, IDeviceCatalogService repo, CemsApiService cems)
        {
            _nav = nav;
            _rootShell = rootShell;
            RootViewModel = rootShell;

            _nav.AttachRootShell(this);

            _repo = repo;
            _cems = cems;
        }

        public async Task InitializeAsync()
        {
            // TopShell
            await _nav.SwitchTopShell<UserShellViewModel>();
            // SubShell
            await _nav.SwitchSubShell<MenuSubShellViewModel>();
        }

        public void SetTopShell(ITopShellHost shell)
        {
            RootViewModel = shell;
        }

        [RelayCommand] private void F0() { }

        [RelayCommand]
        private void F1()
        {
            Trace.WriteLine($"TOPSHELL      [{_nav.ActiveTopShell}]");
            Trace.WriteLine($"SUBSHELL      [{_nav.ActiveSubShell}]");
            Trace.WriteLine($"VIEW          [{_nav.ActiveFlowView}]");
            Trace.WriteLine($"GLOBAL_POPUP  [{_nav.ActiveTopShell?.PopupContent}] ");
            Trace.WriteLine($"LOCAL_POPUP   [{_nav.ActiveSubShell?.PopupContent}] ");
        }

        [RelayCommand]
        private void F2()
        {
            if (_nav.ActiveTopShell is AdminShellViewModel)
            {
                _nav.SwitchTopShell<UserShellViewModel>();
                _nav.SwitchSubShell<MenuSubShellViewModel>();
            }
            else
            {
                _nav.SwitchTopShell<AdminShellViewModel>();
                _nav.SwitchSubShell<EnvironmentShellViewModel>();
            }
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

        [RelayCommand]
        private void F5()
        {
            var x = _repo.LoadAllAsync();
        }
    }
}
