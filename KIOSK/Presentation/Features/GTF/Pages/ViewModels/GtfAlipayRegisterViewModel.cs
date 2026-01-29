using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Drivers;
using KIOSK.Device.Drivers.E200Z;
using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.API.Gtf;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfAlipayRegisterViewModel : StepViewModelBase
    {
        private readonly IDeviceManager _deviceManager;
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        public GtfTaxRefundModel Current => _gtfTaxRefundService.Current;

        [ObservableProperty]
        public string phoneNumber = "";

        partial void OnPhoneNumberChanged(string value)
        {
            var digits = new string(value?.Where(char.IsDigit).ToArray()); // 숫자만 허용

            if (digits.Length <= 3) PhoneNumber = digits;
            else if (digits.Length <= 7) PhoneNumber = $"{digits[..3]}-{digits[3..]}";
            else PhoneNumber = $"{digits[..3]}-{digits[3..7]}-{digits[7..]}";
        }

        public GtfAlipayRegisterViewModel(IDeviceManager deviceManager, GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _deviceManager = deviceManager;
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            await _deviceManager.SendAsync("QR1", new DeviceCommand("SCAN_ENABLE"));

            var QR = _deviceManager.GetDevice<QrE200ZDriver>("QR1");
            if (QR is not null)
                QR.Decoded += ScanVoucherQrCodeAsync;
        }

        public override async Task OnUnloadAsync()
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
            AlipayConfirmRequestDto tmp = new AlipayConfirmRequestDto()
            {
                KioskNo = _gtfTaxRefundService.Current.KioskNo,
                KioskType = _gtfTaxRefundService.Current.KioskType,
                Edi = _gtfTaxRefundService.Current.Edi,
                RefundTypeCode = "02",  // 송금
                RefundWayCode = "05",   // AIPAY
                AlipaySendType = "03",  // QR
                AlipayId = msg.Text,
            };

            // Request API
            var res = await _gtfApiService.AlipayConfirmAsync(tmp, default);

            // Response API
            if (res.Rc == "0000")
            {
                // 계정 저장
                _gtfTaxRefundService.ApplyAlipayAccount(tmp, res);
            }
            else
            {
                // 에러 메세지 표시
                MessageBox.Show(res.Rm, " ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 스캔 활성화
            await _deviceManager.SendAsync("QR1", new DeviceCommand("SCAN_ENABLE"));
        }

        [RelayCommand]
        private void InputNumber(object key)
        {
            string value = key?.ToString() ?? "";
            string raw = new string(PhoneNumber.Where(char.IsDigit).ToArray()); // 현재 숫자만 추출

            switch (value)
            {
                case "Back":   // ← 뒤로 삭제
                    if (raw.Length > 0) raw = raw[..^1];
                    break;

                case "Clear":  // ← 전체 삭제
                    raw = "";
                    break;

                default:
                    // 숫자(0~9)만 추가
                    if (raw.Count() >= 11) return;

                    if (value.All(char.IsDigit))
                        raw += value;
                    break;
            }

            PhoneNumber = raw; // 자동 하이픈 적용
        }

        #region Commands
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private async Task Next(object? o)
        {
#if !DEBUG
            if(InputNumber.Count() == 11)
            {
                // 전화번호 11자리 확인 메세지
                return;
            }

            // QR 데이터
            AlipayConfirmRequestDto tmp = new AlipayConfirmRequestDto()
            {
                KioskNo = _gtfTaxRefundService.Current.KioskNo,
                KioskType = _gtfTaxRefundService.Current.KioskType,
                Edi = _gtfTaxRefundService.Current.Edi,
                RefundTypeCode = "02",  // 송금
                RefundWayCode = "05",   // AIPAY
                AlipaySendType = "02",  // Phone
                AlipayId = InputNumber,
            };

            // Request API
            var res = await _gtfApiService.AlipayConfirmAsync(tmp, default);

            // Response API
            if (res.Rc == "0000")
            {
                // 계정 저장
                _gtfTaxRefundService.ApplyAlipayAccount(tmp, res);
            }
            else
            {
                // 에러 메세지 표시
                MessageBox.Show(res.Rm, " ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
            try
            {
                await ExecuteStepAsync(OnStepNext, PhoneNumber);
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
