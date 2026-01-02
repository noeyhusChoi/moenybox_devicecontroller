using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Modules.GTF.ViewModels;
using KIOSK.Shell.Contracts;
using KIOSK.Services;
using KIOSK.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.FSM;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;

namespace KIOSK.Shell.Sub.Gtf.ViewModel
{
    public partial class GtfSubShellViewModel : ObservableObject, ISubShellHost
    {
        private readonly INavigationService _nav;
        private readonly GtfStateMachine _state;

        public GtfSubShellViewModel(INavigationService nav, GtfStateMachine state)
        {
            _nav = nav;
            _state = state;
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
            await _state.StartAsync();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
