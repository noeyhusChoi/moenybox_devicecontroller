using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Presentation.Features.Exchange.Flow;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Shell.ViewModels
{
    public partial class ExchangeShellViewModel : ObservableObject, IShellHost
    {
        private readonly ExchangeFlowCoordinator _flow;

        public ExchangeShellViewModel(ExchangeFlowCoordinator flow)
        {
            _flow = flow;
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
            await _flow.StartAsync();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
