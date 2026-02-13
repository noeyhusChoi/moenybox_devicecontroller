using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.Menu.Pages.ViewModels;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.Menu.Layout.ViewModels
{
    public partial class MenuLayoutViewModel : ObservableObject, ILayout
    {
        private readonly INavigationService _nav;

        public MenuLayoutViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        [ObservableProperty]
        private object? currentPage;

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
