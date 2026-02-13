using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Features.Startup.Pages.ViewModels;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.Startup.Layout.ViewModels
{
    public partial class StartupLayoutViewModel : ObservableObject, ILayout
    {
        private readonly INavigationService _navigation;

        public StartupLayoutViewModel(INavigationService navigation)
        {
            _navigation = navigation;
        }

        [ObservableProperty]
        private object? currentPage;

        [ObservableProperty]
        private object? popupContent;

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _navigation.NavigatePage<StartupViewModel>();
        }

        public Task OnUnloadAsync()
        {
            return Task.CompletedTask;
        }
    }
}
