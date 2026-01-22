using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Shell.Contracts;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Services;
using KIOSK.Presentation.Shell.Top.Main.ViewModels;
using KIOSK.ViewModels;

namespace KIOSK.Presentation.Shell.Top.Admin.ViewModels
{
    public partial class AdminShellViewModel : ObservableObject, ITopShellHost
    {
        private readonly INavigationService _nav;
        private readonly IInactivityService _inactivityService;

        [ObservableProperty]
        private object? currentShell;

        [ObservableProperty]
        private object? footerViewModel;

        [ObservableProperty]
        private object? popupContent;

        public AdminShellViewModel(INavigationService nav, IInactivityService inactivityService, FooterViewModel footerViewModel)
        {
            _nav = nav;
            _inactivityService = inactivityService;
            FooterViewModel = footerViewModel;
        }

        public void SetShell(object? shell)
        {
            CurrentShell = shell;
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

