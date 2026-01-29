using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.Presentation.Shared.Abstractions;
using KIOSK.Presentation.Features.Exchange.Pages.ViewModels.Popup;
using KIOSK.Presentation.Navigation.Services;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels
{
    public partial class ExchangeIDScanGuideViewModel : StepViewModelBase
    {
        private readonly IExchangeIdScanGuideUseCase _scanGuideUseCase;
        private readonly IPopupService _popup;
        private CancellationTokenSource? _scanCts;

        public ExchangeIDScanGuideViewModel(
            IExchangeIdScanGuideUseCase scanGuideUseCase,
            IPopupService popup)
        {
            _scanGuideUseCase = scanGuideUseCase;
            _popup = popup;
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken pageCt)
        {
            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(pageCt);
            var ct = _scanCts.Token;

            _popup.ShowPopup<ExchangePopupIDScanInfoViewModel>();

            var scanTask = _scanGuideUseCase.ScanUntilStableAsync(ct);
            var timeoutTask = Task.Delay(10000, ct);
            var completed = await Task.WhenAny(scanTask, timeoutTask);

            if (ct.IsCancellationRequested)
                return;

            if (completed == scanTask)
            {
                var success = false;
                try { success = await scanTask; }
                catch (OperationCanceledException) { }

                if (ct.IsCancellationRequested)
                    return;

                _popup.ClosePopup();
                await Task.Delay(150);

                if (success)
                {
                    if (!ct.IsCancellationRequested)
                        await Next();
                }
                else
                {
                    if (!ct.IsCancellationRequested)
                        await Previous();
                }

                return;
            }

            _scanCts.Cancel();

            try { await scanTask; } catch { }

            await _scanGuideUseCase.StopAsync(CancellationToken.None);
            _popup.ClosePopup();

            if (!ct.IsCancellationRequested)
                await Previous();
        }

        public override async Task OnUnloadAsync()
        {
            _scanCts?.Cancel();
            await _scanGuideUseCase.StopAsync(CancellationToken.None);
            _scanCts?.Dispose();
            _scanCts = null;
        }

        [RelayCommand]
        private Task Main() => ExecuteStepAsync(OnStepMain);

        [RelayCommand]
        private Task Previous() => ExecuteStepAsync(OnStepPrevious);

        [RelayCommand]
        private Task Next() => ExecuteStepAsync(OnStepNext);
    }
}
