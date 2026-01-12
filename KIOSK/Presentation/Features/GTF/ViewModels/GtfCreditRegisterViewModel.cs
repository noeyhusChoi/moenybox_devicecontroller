using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Device.Core;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.ViewModels;
using static QRCoder.PayloadGenerator;

namespace KIOSK.Presentation.Features.GTF.ViewModels
{
    public partial class GtfCreditRegisterViewModel : ObservableObject, IStepMain, IStepNext, IStepPrevious, IStepError
    {
        private readonly GtfApiService _gtfApiService;
        private readonly IGtfTaxRefundService _gtfTaxRefundService;

        [ObservableProperty]
        private string cardNumber = ""; // 전체 16자리

        [ObservableProperty]
        private string card1;

        [ObservableProperty]
        private string card2;

        [ObservableProperty]
        private string card3;

        [ObservableProperty]
        private string card4;

        public GtfCreditRegisterViewModel(GtfApiService gtfApiService, IGtfTaxRefundService gtfTaxRefundService)
        {
            _gtfApiService = gtfApiService;
            _gtfTaxRefundService = gtfTaxRefundService;
        }

        partial void OnCardNumberChanged(string value)
        {
            var digits = new string(value?.Where(char.IsDigit).ToArray()); // 숫자만 허용

            Card1 = digits.Length > 0 ? digits[..Math.Min(4, digits.Length)] : "";
            Card2 = digits.Length > 4 ? digits.Substring(4, Math.Min(4, digits.Length - 4)) : "";
            Card3 = digits.Length > 8 ? digits.Substring(8, Math.Min(4, digits.Length - 8)) : "";
            Card4 = digits.Length > 12 ? digits.Substring(12, Math.Min(4, digits.Length - 12)) : "";
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

        [RelayCommand]
        private void InputNumber(object key)
        {
            string value = key?.ToString() ?? "";
            string raw = new string(CardNumber.Where(char.IsDigit).ToArray()); // 현재 숫자만 추출

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
                    if (raw.Count() >= 16) return;

                    if (value.All(char.IsDigit))
                        raw += value;
                    break;
            }

            CardNumber = Format(raw); // 자동 하이픈 적용

            string Format(string raw)
            {
                if (raw.Length <= 3) return raw;
                else if (raw.Length <= 7) return $"{raw[..3]}-{raw[3..]}";
                else return $"{raw[..3]}-{raw[3..7]}-{raw[7..]}";
            }
        }
        #endregion
    }
}
