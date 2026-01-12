using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Infrastructure.UI;
using KIOSK.Infrastructure.UI.Navigation;
using KIOSK.Infrastructure.OCR;
using KIOSK.ViewModels.Exchange.Popup;

namespace KIOSK.ViewModels
{


    public partial class ExchangeIDScanGuideViewModel
        : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IOcrService _ocr;
        private readonly IPopupService _popup;

        private CancellationTokenSource? _scanCts;

        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public ExchangeIDScanGuideViewModel(
            IDeviceManager deviceManager,
            IOcrService ocr,
            IPopupService popup)
        {
            _deviceManager = deviceManager;
            _ocr = ocr;
            _popup = popup;
        }

        //   PAGE LOAD
        public async Task OnLoadAsync(object? parameter, CancellationToken pageCt)
        {
            // pageCt + 자체 CTS를 링크 (하나의 토큰으로 통합)
            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(pageCt);
            var ct = _scanCts.Token;

            // 팝업 표시
            _popup.ShowLocal<ExchangePopupIDScanInfoViewModel>();

            var scanTask = ScanUntilStableAsync(ct);
            var timeoutTask = Task.Delay(10000, ct);

            var completed = await Task.WhenAny(scanTask, timeoutTask);

            // 페이지 이동으로 pageCt가 취소되었는지 확인
            if (ct.IsCancellationRequested)
                return;

            // 스캔 성공
            if (completed == scanTask)
            {
                CommandResult? scanRes = null;

                try { scanRes = await scanTask; }
                catch (OperationCanceledException) { }

                if (ct.IsCancellationRequested)
                    return;

                if (scanRes?.Success == true)
                {
                    _popup.CloseLocal();
                    await Task.Delay(150);

                    if (!ct.IsCancellationRequested)
                        await Next(true);

                    return;
                }

                _popup.CloseLocal();

                if (!ct.IsCancellationRequested)
                    await Previous();

                return;
            }

            // 타임아웃
            _scanCts.Cancel();

            try { await scanTask; } catch { }

            await SafeScanStop();

            _popup.CloseLocal();

            if (!ct.IsCancellationRequested)
                await Previous();
        }

        public async Task OnUnloadAsync()
        {
            _scanCts?.Cancel();
            await SafeScanStop();
            _scanCts?.Dispose();
            _scanCts = null;
        }

        private async Task SafeScanStop()
        {
            try
            {
                await _deviceManager
                    .SendAsync("IDSCANNER1", new DeviceCommand("ScanStop"))
                    .WaitAsync(TimeSpan.FromMilliseconds(300));
            }
            catch
            {
            }
        }

        //   스캔 루프 로직 (백그라운드)
        private async Task<CommandResult?> ScanUntilStableAsync(CancellationToken ct)
        {
            int stableCount = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                // ScanStart
                var startRes = await _deviceManager
                    .SendAsync("IDSCANNER1", new DeviceCommand("ScanStart"))
                    .WaitAsync(ct);

                if (startRes == null || !startRes.Success)
                {
                    await Task.Delay(150, ct);
                    continue;
                }

                // Get Scan Status
                var status = await _deviceManager
                    .SendAsync("IDSCANNER1", new DeviceCommand("GetScanStatus"))
                    .WaitAsync(ct);

                switch ((Pr22.Util.PresenceState)status?.Data)
                {
                    case Pr22.Util.PresenceState.Empty:
                    case Pr22.Util.PresenceState.Dirty:
                    case Pr22.Util.PresenceState.Moving:
                        stableCount = 0;
                        break;

                    case Pr22.Util.PresenceState.Present:
                    case Pr22.Util.PresenceState.NoMove:
                        if (++stableCount >= 5)
                            return status;
                        break;
                }

                await Task.Delay(200, ct);
            }
        }

        //   COMMANDS
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
