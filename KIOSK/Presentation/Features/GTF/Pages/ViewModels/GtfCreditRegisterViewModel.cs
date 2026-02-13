using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Services;
using KIOSK.Application.Services.API;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfCreditRegisterViewModel : PageViewModelBase
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

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            CardNumber = "";
            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync()
        {
            CardNumber = "";
            return Task.CompletedTask;
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
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);

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
