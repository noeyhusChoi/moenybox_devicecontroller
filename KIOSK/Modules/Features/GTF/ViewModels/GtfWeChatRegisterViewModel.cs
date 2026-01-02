using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Drivers.E200Z;
using KIOSK.Devices.Management;
using KIOSK.Services;
using KIOSK.Services.API;
using KIOSK.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KIOSK.Modules.GTF.ViewModels
{
    public partial class GtfWeChatRegisterViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError, INavigable
    {
        private readonly IDeviceManager _deviceManager;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfWeChatRegisterViewModel(IDeviceManager deviceManager, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _deviceManager = deviceManager;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _deviceManager.SendAsync("QR1", new DeviceCommand("SCAN_ENABLE"));

            var QR = _deviceManager.GetDevice<QrE200ZDriver>("QR1");
            if (QR is not null)
                QR.Decoded += ScanVoucherQrCodeAsync;
        }

        public async Task OnUnloadAsync()
        {
            await _deviceManager.SendAsync("QR1", new DeviceCommand("SCAN_DISABLE"));

            var QR = _deviceManager.GetDevice<QrE200ZDriver>("QR1");
            if (QR is not null)
                QR.Decoded -= ScanVoucherQrCodeAsync;
        }

        // QR 코드 스캔 처리 메서드
        private async void ScanVoucherQrCodeAsync(object? sender, DecodeMessage msg)
        {
            // 스캔 중지
            await _deviceManager.SendAsync("QR1", new DeviceCommand("SCAN_DISABLE"));
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
            await _deviceManager.SendAsync("QR1", new DeviceCommand("SCAN_ENABLE"));
        }

        #region Commands
        public Func<Task>? OnStepMain { get; set; }
        public Func<Task>? OnStepPrevious { get; set; }
        public Func<string?, Task>? OnStepNext { get; set; }
        public Action<Exception>? OnStepError { get; set; }


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
