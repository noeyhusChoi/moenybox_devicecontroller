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
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfWeChatRegisterViewModel : PageViewModelBase
    {
        private readonly IQrScannerPort _qrScannerPort;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfWeChatRegisterViewModel(IQrScannerPort qrScannerPort, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _qrScannerPort = qrScannerPort;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _qrScannerPort.Decoded += ScanVoucherQrCodeAsync;
            await _qrScannerPort.EnableAsync("QR1", ct);
        }

        public override async Task OnUnloadAsync()
        {
            await _qrScannerPort.DisableAsync("QR1");
            _qrScannerPort.Decoded -= ScanVoucherQrCodeAsync;
        }

        // QR 코드 스캔 처리 메서드
        private async void ScanVoucherQrCodeAsync(object? sender, QrDecodedEventArgs msg)
        {
            // 스캔 중지
            await _qrScannerPort.DisableAsync("QR1");
            Trace.WriteLine($"Scanned QR Code :TYPE[{msg.BarcodeType:X2}] TEXT[{msg.Text}]");

            // QR 데이터
            WechatRefundRequestDto req = new WechatRefundRequestDto
            {
                KioskNo = _gtfTaxRefundService.Current.KioskNo,
                KioskType = _gtfTaxRefundService.Current.KioskType,
                Edi = _gtfTaxRefundService.Current.Edi,
                RefundTypeCode = "02",
                RefundWayCode = "18",
                RefundNo = "",
                BuySerialNo = _gtfTaxRefundService.Current.SlipItems.Select(x => x.BuySerialNo).ToArray(),
                NumberOfSlip = _gtfTaxRefundService.Current.SlipItems.Select(x => x.QrData).Distinct().Count().ToString(),
                WechatMiniBarcode = msg.Text,
            };

            // Request API
            var res = await _gtfApiService.WechatRefundAsync(req, default);

            // Response API
            if (res.Rc == "0000")
            {
                // 결과 저장, 화면 표시
                _gtfTaxRefundService.ApplyWechatRefund(req, res);
            }
            else
            {
                // 에러 메세지 표시
                MessageBox.Show(res.Rm, " ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 스캔 활성화
            await _qrScannerPort.EnableAsync("QR1");
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
