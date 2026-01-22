using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Shell.Contracts;
using KIOSK.Application.Services;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.MenuV2.ViewModels;

namespace KIOSK.Presentation.Features.MenuV2.Shell.ViewModels
{
    public partial class MenuV2ShellViewModel : ObservableObject, IShellHost
    {
        private readonly INavigationService _nav;

        public MenuV2ShellViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        [ObservableProperty]
        private object? currentView;

        public void SetInnerView(object view)
        {
            CurrentView = view;
        }

        [ObservableProperty]
        private object? popupContent;

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _nav.NavigateTo<MenuV2ViewModel>();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
