using KIOSK.Presentation.Features.GTF.Flow;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Layout.ViewModels
{
    public sealed class GtfLayoutViewModel : LayoutViewModelBase
    {
        private readonly Gtf _flow;

        public GtfLayoutViewModel(Gtf flow)
        {
            _flow = flow;
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _flow.StartAsync();
        }

        public override async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }
    }
}
