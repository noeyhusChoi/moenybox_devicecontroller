using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Navigation.Services;
using KIOSK.Presentation.Shared.Abstractions;
using KIOSK.Presentation.Features.Environment.Pages.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.Environment.Shell.ViewModels
{
    public partial class EnvironmentShellViewModel : ObservableObject, ILayout
    {
        private readonly INavigationService _nav;

        public EnvironmentShellViewModel(INavigationService nav)
        {
            _nav = nav;
        }

        [ObservableProperty]
        private object? currentView;

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
