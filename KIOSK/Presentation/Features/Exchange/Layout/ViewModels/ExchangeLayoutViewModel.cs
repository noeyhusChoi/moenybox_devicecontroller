using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Presentation.Features.Exchange.Flow;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Layout.ViewModels
{
    public partial class ExchangeLayoutViewModel : ObservableObject, ILayout
    {
        private readonly ExchangeFlowCoordinator _flow;

        public ExchangeLayoutViewModel(ExchangeFlowCoordinator flow)
        {
            _flow = flow;
        }

        [ObservableProperty]
        private object? currentPage;

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
