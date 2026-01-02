using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Shell.Contracts;
using KIOSK.Services;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;

namespace KIOSK.Shell.Top.Main.ViewModels
{
    public partial class UserShellViewModel : ObservableObject, ITopShellHost
    {
        private readonly INavigationService _nav;
        private readonly IInactivityService _inactivityService;

        [ObservableProperty]
        private object currentSubShell;

        [ObservableProperty]
        private object footerViewModel;

        [ObservableProperty]
        private object? popupContent;

        public UserShellViewModel(INavigationService nav, IInactivityService inactivityService, FooterViewModel footerViewModel)
        {
            _nav = nav;
            _inactivityService = inactivityService; // Update to use injected service

            FooterViewModel = footerViewModel; // 푸터 고정
        }

        public void SetSubShell(object? shell)
        {
            CurrentSubShell = shell;
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;

        }
    }
}
