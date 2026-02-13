using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Abstractions;
using KIOSK.Presentation.Features.Environment.Pages.ViewModels;

namespace KIOSK.Presentation.Features.Environment.Layout.ViewModels
{
    public partial class EnvironmentLayoutViewModel : ObservableObject, ILayout
    {
        private readonly INavigationService _nav;

        public EnvironmentLayoutViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        [ObservableProperty]
        private object? currentPage;

        [ObservableProperty]
        private object? popupContent;

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _nav.NavigatePage<EnvironmentViewModel>();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
