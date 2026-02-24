using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Application.Services.Devices;
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Infrastructure.OCR;
using KIOSK.Infrastructure.OCR.Models;
using KIOSK.Presentation.Abstractions;
using Pr22.Processing;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfIdScanProcessViewModel : PageViewModelBase
    {
        private const string IdScannerDeviceId = "IDSCANNER1";


        private readonly IIdScannerPort _idScannerPort;

        private readonly IOcrService _ocrService;

        private readonly GtfApiService _gtfApiService;

        private readonly IGtfTaxRefundService _gtfTaxRefundService;



        public GtfIdScanProcessViewModel(
            IIdScannerPort idScannerPort,
            IOcrService ocrService,
            GtfApiService gtfApiService,
            IGtfTaxRefundService gtfTaxRefundService)
        {
            _idScannerPort = idScannerPort;
            _ocrService = ocrService;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _ = Task.Run(() => InitAsync(ct), ct);
            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;



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



                    await App.Current.Dispatcher.InvokeAsync(() => ExecuteStepAsync(OnStepNext)).Task;

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

                var page = await _idScannerPort
                    .SaveImageAsync(IdScannerDeviceId, ct)
                    .ConfigureAwait(false);

                if (page is not null)
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



        private Task GoPreviousAsync() =>
            App.Current.Dispatcher.InvokeAsync(() => ExecuteStepAsync(OnStepPrevious)).Task;





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
