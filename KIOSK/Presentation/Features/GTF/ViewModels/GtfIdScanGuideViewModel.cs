using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Media;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Application.Services;
using KIOSK.Infrastructure.OCR;
using KIOSK.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace KIOSK.Presentation.Features.GTF.ViewModels
{
    public partial class GtfIdScanGuideViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IOcrService _ocr;
        private readonly IVideoPlayService _videoPlayService;
        public Brush? BackgroundBrush => _videoPlayService.BackgroundBrush;

        private Uri videoPath;

        private CancellationTokenSource? _scanCts;

        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public GtfIdScanGuideViewModel(IDeviceManager deviceManager, IOcrService ocr, IVideoPlayService videoPlayService)
        {
            _deviceManager = deviceManager;
            _ocr = ocr;
            _videoPlayService = videoPlayService;
            
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

            _videoPlayService.SetSource(videoPath, loop: true, mute: true, autoPlay: true);
        }

        public Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            // 혹시 이전 인스턴스가 남아있으면 정리
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // 연결된 토큰으로 백그라운드 실행
            _ = RunScanFlowAsync(_scanCts.Token);
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
            if (_scanCts is not null)
            {
                _scanCts.Cancel();
                _scanCts.Dispose();
                _scanCts = null;
            }

            _deviceManager.SendAsync("IDSCANNER1", new DeviceCommand("ScanStop"));

            return Task.CompletedTask;
        }

        private async Task RunScanFlowAsync(CancellationToken ct)
        {
            try
            {
                var result = await ScanUntilStableAsync(ct);

                if (result is not null && OnStepNext is not null)
                {
                    await OnStepNext("");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
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
                var res = await _deviceManager
                    .SendAsync("IDSCANNER1", new DeviceCommand("ScanStart"))
                    .WaitAsync(ct);

                if (res == null || res.Success == false)
                {
                    res = await _deviceManager
                    .SendAsync("IDSCANNER1", new DeviceCommand("ScanStart"))
                    .WaitAsync(ct);
                }
                else
                {
                    var status = await _deviceManager
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
        private async Task Main()
        {
            try
            {
                if (OnStepMain is not null)
                    await OnStepMain();
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }

        [RelayCommand]
        private async Task Previous()
        {
            try
            {
                if (OnStepPrevious is not null)
                    await OnStepPrevious();
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }

        [RelayCommand]
        private async Task Next(object? o)
        {
            try
            {
                if (OnStepNext is not null)
                    await OnStepNext("");
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }
        #endregion
    }
}
