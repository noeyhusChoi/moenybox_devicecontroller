using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.ViewModels;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.Menu.Shell.ViewModels
{
    public partial class MenuShellViewModel : ObservableObject, IShellHost
    {
        private readonly INavigationService _nav;

        public MenuShellViewModel(INavigationService nav)
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
            await _nav.NavigateTo<MenuViewModel>();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
