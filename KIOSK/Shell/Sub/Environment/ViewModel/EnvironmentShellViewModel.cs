using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Shell.Contracts;
using KIOSK.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Shell.Sub.Environment.ViewModel
{
    public partial class EnvironmentShellViewModel : ObservableObject, ISubShellHost
    {
        private readonly INavigationService _nav;

        public EnvironmentShellViewModel(INavigationService nav)
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
            await _nav.NavigateTo<EnvironmentViewModel>();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
