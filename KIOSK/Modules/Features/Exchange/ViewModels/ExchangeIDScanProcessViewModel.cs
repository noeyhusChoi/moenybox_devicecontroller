using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Infrastructure.Media;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Management;
using KIOSK.Services;
using KIOSK.Services.OCR;
using KIOSK.Services.OCR.Models;
using Pr22.Processing;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace KIOSK.ViewModels
{
    public partial class ExchangeIDScanProcessViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        private Uri videoPath;

        [ObservableProperty]
        private Brush backgroundBrush;

        private readonly IDeviceManager _deviceManager;
        private readonly IOcrService _ocr;
        private readonly ITransactionServiceV2 _transaction;
        private readonly IVideoPlayService _videoPlay;

        public ExchangeIDScanProcessViewModel(IDeviceManager deviceManager, IOcrService ocr, ITransactionServiceV2 transaction, IVideoPlayService videoPlay)
        {
            _deviceManager = deviceManager;
            _ocr = ocr;
            _transaction = transaction;
            _videoPlay = videoPlay;

            try
            {
                // TODO: 파일 존재 유무 체크
                videoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "Loading.mp4"), UriKind.Absolute);
            }
            catch (IOException ex)
            {
                // 파일을 찾지 못했을 때
                //_logging?.Error(ex, ex.Message);
            }
            catch (Exception ex)
            {
                // 그 외 예외
                //_logging?.Error(ex, ex.Message);
            }

            BackgroundBrush = _videoPlay.BackgroundBrush;
            _videoPlay.SetSource(videoPath, loop: true, mute: true, autoPlay: true);
        }

        public Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _ = Task.Run(() => InitAsync(ct), ct);
            return Task.CompletedTask;
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        private async Task InitAsync(CancellationToken ct)
        {
            var result = await _deviceManager.SendAsync("IDSCANNER1", new DeviceCommand("SaveImage"));

            if (result?.Data is Page page)
            {
                try
                {
                    var outcome = await _ocr.RunAsync(page, OcrMode.Auto, CancellationToken.None);

                    if (outcome.Success)
                    {
                        foreach (var value in outcome.Fields)
                            Trace.WriteLine($"{value}");

                        // 0) OCR 데이터 저장
                        await _transaction.UpsertCustomerAsync(outcome.DocumentType, outcome.Fields["NAME"], outcome.Fields["NO"], outcome.Fields["NATIONALITY"]);

                        // 1) 스캔 원본 내부 리소스 해제 (Page가 IDisposable이면 dispose)
                        if (page is IDisposable d)
                        {
                            try { d.Dispose(); } catch { /* ignore */ }
                        }

                        // 2) UI가 해제 작업을 실행할 시간 줌
                        await Task.Delay(50, ct).ConfigureAwait(false);

                        // 3) 화면 전환 UI 동작 Dispatcher로 넘기기
                        await App.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await Next(true);
                        });
                    }
                    else
                    {
                        await App.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await Previous();
                        });
                    }
                }
                catch (Exception ex)
                {
                    OnStepError?.Invoke(ex);
                }
                finally
                {
                    // GC.Collect();
                    // GC.WaitForPendingFinalizers();
                }
            }
        }


        [RelayCommand]
        private async Task Loaded(object parameter) // 파라미터 필요없으면 object 대신 없음
        {
            await Task.Run(async () =>
            {
                await _deviceManager.SendAsync("IDSCANNER1", new DeviceCommand("ScanStop"));
                var result = await _deviceManager.SendAsync("IDSCANNER1", new DeviceCommand("SaveImage"));

                if (result?.Data is Page page)
                {
                    try
                    {
                        var outcome = await _ocr.RunAsync(page, OcrMode.Auto, CancellationToken.None);

                        if (outcome.Success)
                        {
                            foreach (var value in outcome.Fields)
                                Trace.WriteLine($"{value}");

                            // 0) OCR 데이터 저장
                            await _transaction.UpsertCustomerAsync(outcome.DocumentType, outcome.Fields["NAME"], outcome.Fields["NO"], outcome.Fields["NATIONALITY"]);

                            // 1) 스캔 원본 내부 리소스 해제 (Page가 IDisposable이면 dispose)
                            if (page is IDisposable d)
                            {
                                try { d.Dispose(); } catch { /* ignore */ }
                            }

                            // 2) UI가 해제 작업을 실행할 시간 줌
                            await Task.Delay(50);

                            // 3) 화면 전환
                            await Next(true);
                        }
                        else
                        {
                            await Previous();
                        }
                    }
                    catch (Exception ex)
                    {
                        OnStepError?.Invoke(ex);
                    }
                    finally
                    {
                        // GC.Collect();
                        // GC.WaitForPendingFinalizers();
                    }
                }
            }).ConfigureAwait(false);
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
