using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Shell.Contracts;
using KIOSK.Services;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;
using KIOSK.Shell.Top.Main.ViewModels;
using KIOSK.ViewModels;

namespace KIOSK.Shell.Top.Admin.ViewModels
{
    public partial class AdminShellViewModel : ObservableObject, ITopShellHost
    {
        private readonly INavigationService _nav;
        private readonly IInactivityService _inactivityService;

        [ObservableProperty]
        private object currentSubShell;

        [ObservableProperty]
        private object footerViewModel;

        [ObservableProperty]
        private object? popupContent;

        public AdminShellViewModel(INavigationService nav, IInactivityService inactivityService, FooterViewModel footerViewModel)
        {
            _nav = nav;
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

