using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Navigation.Services;
using System;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Popup.ViewModels
{
    public partial class ExchangeV2IdScanFailedPopupViewModel : ObservableObject
    {
        private readonly IPopupService _popup;

        [ObservableProperty]
        private string title = "신분증 인식에 실패했습니다.";

        [ObservableProperty]
        private string message = "확인 후 다시 동의 화면으로 이동해 재시도해 주세요.";

        public Func<Task>? OnConfirmAsync { get; set; }

        public ExchangeV2IdScanFailedPopupViewModel(IPopupService popup)
        {
            _popup = popup;
        }

        [RelayCommand]
        private async Task Confirm()
        {
            _popup.ClosePopup();

            if (OnConfirmAsync is not null)
                await OnConfirmAsync();
        }
    }
}
