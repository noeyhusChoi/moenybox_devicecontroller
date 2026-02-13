using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.Devices;
using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.OCR;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfIdScanGuideViewModel : PageViewModelBase
    {
        private readonly IDeviceCommandService _deviceCommandService;
        private readonly IOcrService _ocr;
        private Uri videoPath;
        private CancellationTokenSource? _scanCts;
        
        public GtfIdScanGuideViewModel(IDeviceCommandService deviceCommandService, IOcrService ocr)
        {
            _deviceCommandService = deviceCommandService;
            _ocr = ocr;
            
            try
            {
                // TODO: 파일 존재 유무 체크
                videoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "IDScan_Passport.mp4"), UriKind.Absolute);
            }

            catch (IOException)
            {
                // 파일을 찾지 못했을 때
                //_logging?.Error(ex, ex.Message);
            }
            catch (Exception)
            {
                // 그 외 예외
                //_logging?.Error(ex, ex.Message);
            }
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // 혹시 이전 인스턴스가 남아있으면 정리
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // 연결된 토큰으로 백그라운드 실행
            _ = RunScanFlowAsync(_scanCts.Token);
            return Task.CompletedTask;
        }
        public override Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
            if (_scanCts is not null)
            {
                _scanCts.Cancel();
                _scanCts.Dispose();
                _scanCts = null;
            }

            _deviceCommandService.SendAsync("IDSCANNER1", new DeviceCommand("ScanStop"));

            return Task.CompletedTask;
        }

        private async Task RunScanFlowAsync(CancellationToken ct)
        {
            try
            {
                var result = await ScanUntilStableAsync(ct);

                if (result is not null)
                {
                    await ExecuteStepAsync(OnStepNext, result);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }
        
        private async Task<CommandResult?> ScanUntilStableAsync(CancellationToken ct)
        {
            int maintainCount = 0;
            Trace.WriteLine("SCAN___START");
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                // SendAsync가 ct를 받지 못하면 .WaitAsync(ct)로 감싸기
                var res = await _deviceCommandService
                    .SendAsync("IDSCANNER1", new DeviceCommand("ScanStart"))
                    .WaitAsync(ct);

                if (res == null || res.Success == false)
                {
                    res = await _deviceCommandService
                    .SendAsync("IDSCANNER1", new DeviceCommand("ScanStart"))
                    .WaitAsync(ct);
                }
                else
                {
                    var status = await _deviceCommandService
                    .SendAsync("IDSCANNER1", new DeviceCommand("GetScanStatus"))
                    .WaitAsync(ct);

                    if (status?.Data is Pr22.Util.PresenceState state)
                    {
                        switch (state)
                        {
                            case Pr22.Util.PresenceState.Empty:
                            case Pr22.Util.PresenceState.Dirty:
                            case Pr22.Util.PresenceState.Moving:
                                if (maintainCount > 0) maintainCount = 0;
                                break;

                            case Pr22.Util.PresenceState.Present:
                            case Pr22.Util.PresenceState.NoMove:
                                if (++maintainCount > 5) return status;
                                break;
                        }
                    }
                }

                await Task.Delay(200, ct);
            }
        }

        #region Commands

        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
        #endregion
    }
}
