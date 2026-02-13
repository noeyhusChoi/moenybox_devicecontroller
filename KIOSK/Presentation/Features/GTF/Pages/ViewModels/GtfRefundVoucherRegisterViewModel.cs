using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Application.Services.Devices;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfRefundVoucherRegisterViewModel : PageViewModelBase
    {
        private readonly IQrScannerDeviceService _qrScannerDeviceService;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfTaxRefundModel Current => _gtfTaxRefundService.Current;

        public GtfRefundVoucherRegisterViewModel(IQrScannerDeviceService qrScannerDeviceService, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _qrScannerDeviceService = qrScannerDeviceService;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _qrScannerDeviceService.Decoded += ScanVoucherQrCodeAsync;
            await _qrScannerDeviceService.EnableAsync("QR1", ct);
        }

        public override async Task OnUnloadAsync()
        {
            await _qrScannerDeviceService.DisableAsync("QR1");
            _qrScannerDeviceService.Decoded -= ScanVoucherQrCodeAsync;
        }

        // QR 코드 스캔 처리 메서드
        private async void ScanVoucherQrCodeAsync(object? sender, QrDecodedEventArgs msg)
        {
            // 스캔 중지
            await _qrScannerDeviceService.DisableAsync("QR1");
            Trace.WriteLine($"Scanned QR Code :TYPE[{msg.BarcodeType:X2}] TEXT[{msg.Text}]");

            // QR 데이터
            RegisterSlipRequestDto req = new RegisterSlipRequestDto()
            {
                KioskNo = _gtfTaxRefundService.Current.KioskNo,
                KioskType = _gtfTaxRefundService.Current.KioskType,
                Edi = _gtfTaxRefundService.Current.Edi,
                RefundTypeCode = "02",
                PassportNo = _gtfTaxRefundService.Current.PassportNo,
                NationalityCode = _gtfTaxRefundService.Current.NationalityCode,
                PassportSerialNo = _gtfTaxRefundService.Current.PassportSerialNo,
                QrDataType = "02",
                QrData = msg.Text.Substring(0, 20),

            };

            // Request API
            var res = await _gtfApiService.RegisterSlipAsync(req, default);

            // Response API
            if (res.Rc == "0000")
            {
                // 결과 저장, 화면 표시
                _gtfTaxRefundService.AddSlip(req, res);
                Trace.WriteLine($"등록된 바우처 개수: {_gtfTaxRefundService.Current.SlipItems.Select(x => x.QrData).Distinct().Count()}");
            }
            else
            {
                // 에러 메세지 표시
                MessageBox.Show(res.Rm, " ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 스캔 활성화
            await _qrScannerDeviceService.EnableAsync("QR1");
        }

        private async Task<CustomsResultResponseDto> CustomsResult()
        {
            CustomsResultRequestDto tmp = new CustomsResultRequestDto()
            {
                KioskNo = _gtfTaxRefundService.Current.KioskNo,
                KioskType = _gtfTaxRefundService.Current.KioskType,
                Edi = _gtfTaxRefundService.Current.Edi,
                BuySerialNo = _gtfTaxRefundService.Current.SlipItems.Select(x => x.BuySerialNo).ToArray(),
                NumberOfSlip = _gtfTaxRefundService.Current.SlipItems.Select(x => x.QrData).Distinct().Count().ToString(),
            };

            return await _gtfApiService.CustomsResultAsync(tmp, default);
        }

        private async Task<DepositAmtResponseDto> DepositAmt()
        {
            DepositAmtRequestDto tmp = new DepositAmtRequestDto()
            {
                KioskNo = _gtfTaxRefundService.Current.KioskNo,
                KioskType = _gtfTaxRefundService.Current.KioskType,
                Edi = _gtfTaxRefundService.Current.Edi,
                BuySerialNo = _gtfTaxRefundService.Current.SlipItems.Select(x => x.BuySerialNo).ToArray(),
                NumberOfSlip = _gtfTaxRefundService.Current.SlipItems.Select(x => x.QrData).Distinct().Count().ToString(),
            };

            return await _gtfApiService.DepositAmtAsync(tmp, default);
        }

        #region Commands
        [RelayCommand]
        private async Task Main(object? parameter)
        {
            // 취소 요청
            var res = await _gtfApiService.CustomsCancelAsync(new CustomsCancelRequestDto()
            {
                KioskNo = _gtfTaxRefundService.Current.KioskNo,
                KioskType = _gtfTaxRefundService.Current.KioskType,
                Edi = _gtfTaxRefundService.Current.Edi,
                BuySerialNo = _gtfTaxRefundService.Current.SlipItems.Select(x => x.BuySerialNo).ToArray(),
                NumberOfSlip = _gtfTaxRefundService.Current.SlipItems.Select(x => x.QrData).Distinct().Count().ToString(),
            }, default);

            if (res.Rc == "0000")
            {
                // DB Pending -> Cancel
            }
            else
            {
                // DB Pending -> Cancel_Requests
            }

            try
            {
                await ExecuteStepAsync(OnStepMain, parameter);
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    await RaiseStepErrorAsync(ex);
            }
        }

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);


        [RelayCommand]
        private async Task Next(object? parameter)
        {
            try
            {
#if !DEBUG
                // 1) 바우처 없으면 안내 후 종료
                if (Current.SlipItems.Count == 0)
                {
                    MessageBox.Show("환급전표를 등록해주세요", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    // TODO: 영수증(바우처) 등록 안내 메시지
                    return;
                }

                // 2) 반출 요청
                var customsRes = await CustomsResult();
                if (customsRes.Rc != "0000")
                {
                    // 에러 메세지 표시
                    MessageBox.Show(customsRes.Rc, customsRes.Rc, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3) 담보금 계산
                var depositRes = await DepositAmt();
                if (depositRes.Rc != "0000")
                {
                    // 에러 메세지 표시
                    MessageBox.Show(depositRes.Rc, customsRes.Rc, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
#endif
                // 4) 다음 화면 선택 (의료,숙박 체크)
                string nextStep = Current.SlipItems.Any(x => x.HotelRefundYn == "Y" || x.MediRefundYn == "Y") ? "Sign" : _gtfTaxRefundService.Current.SelectedRefundWayCode;

                // 5) OnStepNext 실행
                await ExecuteStepAsync(OnStepNext, nextStep);
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }
        #endregion
    }
}
