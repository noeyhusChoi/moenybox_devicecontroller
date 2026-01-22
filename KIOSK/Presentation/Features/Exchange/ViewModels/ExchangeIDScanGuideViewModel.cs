using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services.Exchange;
using KIOSK.Presentation.Navigation.Popup;
using KIOSK.Presentation.Shared.Abstractions;
using KIOSK.ViewModels.Exchange.Popup;

namespace KIOSK.ViewModels
{
    public partial class ExchangeIDScanGuideViewModel
        : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        private readonly IExchangeIdScanGuideUseCase _scanGuideUseCase;
        private readonly IPopupService _popup;
        private CancellationTokenSource? _scanCts;

        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public ExchangeIDScanGuideViewModel(
            IExchangeIdScanGuideUseCase scanGuideUseCase,
            IPopupService popup)
        {
            _scanGuideUseCase = scanGuideUseCase;
            _popup = popup;
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken pageCt)
        {
            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(pageCt);
            var ct = _scanCts.Token;

            _popup.ShowLocal<ExchangePopupIDScanInfoViewModel>();

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

                _popup.CloseLocal();
                await Task.Delay(150);

                if (success)
                {
                    if (!ct.IsCancellationRequested)
                        await Next(true);
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
            _popup.CloseLocal();

            if (!ct.IsCancellationRequested)
                await Previous();
        }

        public async Task OnUnloadAsync()
        {
            _scanCts?.Cancel();
            await _scanGuideUseCase.StopAsync(CancellationToken.None);
            _scanCts?.Dispose();
            _scanCts = null;
        }

        [RelayCommand]
        private async Task Main() =>
            await (OnStepMain?.Invoke() ?? Task.CompletedTask);

        [RelayCommand]
        private async Task Previous() =>
            await (OnStepPrevious?.Invoke() ?? Task.CompletedTask);

        [RelayCommand]
        private async Task Next(object? _) =>
            await (OnStepNext?.Invoke("") ?? Task.CompletedTask);
    }
}
