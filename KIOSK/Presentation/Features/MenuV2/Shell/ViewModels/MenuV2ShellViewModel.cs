using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Features.MenuV2.Pages.ViewModels;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.MenuV2.Shell.ViewModels
{
    public partial class MenuV2ShellViewModel : ObservableObject, ILayout
    {
        private readonly INavigationService _nav;

        public MenuV2ShellViewModel(INavigationService nav)
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
