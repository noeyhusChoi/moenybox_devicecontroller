using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Shell.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Presentation.Features.GTF.Flow;

namespace KIOSK.Presentation.Features.GTF.Shell.ViewModels
{
    public partial class GtfSubShellViewModel : ObservableObject, ISubShellHost
    {
        private readonly GtfFlowCoordinator _flow;

        public GtfSubShellViewModel(GtfFlowCoordinator flow)
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
