using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.Menu.Pages.ViewModels;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.Menu.Shell.ViewModels
{
    public partial class MenuShellViewModel : ObservableObject, ILayout
    {
        private readonly INavigationService _nav;

        public MenuShellViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        [ObservableProperty]
        private object? currentView;

        [ObservableProperty]
        private object? popupContent;

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _nav.NavigatePage<MenuViewModel>();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
