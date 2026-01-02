using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Abstractions;
using KIOSK.Devices.Management;
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Services;
using KIOSK.Services.API;
using KIOSK.Services.OCR;
using KIOSK.Services.OCR.Models;
using KIOSK.ViewModels;
using Pr22.Processing;
using System.Diagnostics;

namespace KIOSK.Modules.GTF.ViewModels
{
    public partial class GtfIdScanProcessViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        private readonly IDeviceManager _deviceManager;
        private readonly IOcrService _ocrService;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfIdScanProcessViewModel(IDeviceManager deviceManager, IOcrService ocrService, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _deviceManager = deviceManager;
            _ocrService = ocrService;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
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
            Page? page = null;

            try
            {
                if (ct.IsCancellationRequested)
                    return;

                // 1) ID 스캐너 이미지 캡처
                page = await CapturePageAsync(ct).ConfigureAwait(false);
                if (page is null)
                {
                    await GoPreviousAsync().ConfigureAwait(false);
                    return;
                }

                if (ct.IsCancellationRequested)
                    return;

                // 2) OCR 실행
                var outcome = await RunOcrAsync(page, ct).ConfigureAwait(false);
                if (outcome is null || !outcome.Success)
                {
                    await GoPreviousAsync().ConfigureAwait(false);
                    return;
                }

                if (ct.IsCancellationRequested)
                    return;

                // 3) OCR 결과 파싱
                if (!TryBuildInquiryRequest(outcome, out var req))
                {
                    await GoPreviousAsync().ConfigureAwait(false);
                    return;
                }

                if (ct.IsCancellationRequested)
                    return;

                // 4) GTF API 호출
                var res = await CallInquiryApiAsync(req, ct).ConfigureAwait(false);

                if (res?.Rc == "0000")
                {
                    _gtfTaxRefundService.ApplyInquirySlipList(req, res);

                    await Task.Delay(50, ct).ConfigureAwait(false);

                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (OnStepNext is not null)
                            return OnStepNext("");
                        return Task.CompletedTask;
                    });
                }
                else
                {
                    // 비즈니스 오류 코드
                    await GoPreviousAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소는 조용히 무시
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // 필요하면 상세 로그
                // _logging.Error(ex, "[GTF][IdScanProcess] Unexpected error");

                await GoPreviousAsync().ConfigureAwait(false);
            }
            finally
            {
                if (page is IDisposable d)
                    d.Dispose();
            }
        }

        private async Task<Page?> CapturePageAsync(CancellationToken ct)
        {
            try
            {
                var result = await _deviceManager
                    .SendAsync("IDSCANNER1", new DeviceCommand("SaveImage"), ct)
                    .ConfigureAwait(false);

                if (result?.Data is Page page)
                    return page;

                // 비즈니스적으로 "스캔 실패"
                // _logging.Warn("[GTF][IdScan] SaveImage returned no Page");
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 여기서 IDSCANNER 관련 예외 로그
                Debug.WriteLine(ex);
                // _logging.Error(ex, "[GTF][IdScan] IDSCANNER1 SaveImage 실패");
                return null;
            }
        }

        private async Task<OcrOutcome?> RunOcrAsync(Page page, CancellationToken ct)
        {
            try
            {
                var outcome = await _ocrService
                    .RunAsync(page, OcrMode.Auto, ct)
                    .ConfigureAwait(false);

                if (!outcome.Success)
                {
                    // _logging.Warn("[GTF][OCR] OCR 실패");
                    return null;
                }

                // 디버깅용 필드 출력
                foreach (var kv in outcome.Fields)
                    Trace.WriteLine($"{kv.Key} = {kv.Value}");

                return outcome;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // _logging.Error(ex, "[GTF][OCR] OCR 실행 중 예외");
                return null;
            }
        }

        private bool TryBuildInquiryRequest(OcrOutcome outcome, out InquirySlipListRequestDto req)
        {
            req = null!;

            try
            {
                if (!outcome.Fields.TryGetValue("BirthDate", out var birthDate) ||
                    !outcome.Fields.TryGetValue("Sex", out var sex) ||
                    !outcome.Fields.TryGetValue("NAME", out var name) ||
                    !outcome.Fields.TryGetValue("NATIONALITY", out var nationality) ||
                    !outcome.Fields.TryGetValue("ExpiryDate", out var expiryDate) ||
                    !outcome.Fields.TryGetValue("NO", out var passportNo))
                {
                    // _logging.Warn("[GTF][OCR] 필수 필드 누락");
                    return false;
                }

                var current = _gtfTaxRefundService.Current;

                req = new InquirySlipListRequestDto
                {
                    KioskNo = current.KioskNo,
                    KioskType = current.KioskType,
                    Birthday = DateTime.TryParse(birthDate, null, out var birthDt) ? birthDt.ToString("yyMMdd") : string.Empty,
                    GenderCode = sex,
                    Name = name,
                    NationalityCode = nationality,
                    PassportExpirdate = DateTime.TryParse(expiryDate, null, out var expiryDt) ? expiryDt.ToString("yyMMdd") : string.Empty,
                    PassportNo = passportNo,
                };

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // _logging.Error(ex, "[GTF][OCR] 필드 파싱 중 예외");
                return false;
            }
        }

        private async Task<InquirySlipListResponseDto?> CallInquiryApiAsync(
            InquirySlipListRequestDto req,
            CancellationToken ct)
        {
            try
            {
                return await _gtfApiService.InquirySlipListAsync(req, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // _logging.Error(ex, "[GTF][API] InquirySlipList 호출 중 예외");
                return null;
            }
        }

        private Task GoPreviousAsync()
        {
            return App.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (OnStepPrevious is not null)
                    await OnStepPrevious();
            }).Task;
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
