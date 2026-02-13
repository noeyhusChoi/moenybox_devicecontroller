using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.MenuV2.Pages.ViewModels;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.MenuV2.Layout.ViewModels
{
    public partial class MenuV2LayoutViewModel : ObservableObject, ILayout
    {
        private readonly INavigationService _nav;

        public MenuV2LayoutViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        [ObservableProperty]
        private object? currentPage;

        [ObservableProperty]
        private object? popupContent;

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _nav.NavigatePage<MenuV2ViewModel>();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
