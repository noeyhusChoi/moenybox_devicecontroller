using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.FSM;
using KIOSK.Shell.Contracts;
using KIOSK.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Infrastructure.UI.Navigation.Services;
using KIOSK.Infrastructure.UI.Navigation.State;

namespace KIOSK.Shell.Sub.Exchange.ViewModel
{
    public partial class ExchangeShellViewModel : ObservableObject, ISubShellHost
    {
        private readonly INavigationService _nav;
        private readonly ExchangeSellStateMachine _state;

        public ExchangeShellViewModel(INavigationService nav, ExchangeSellStateMachine state)
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
