using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Navigation.Popup;
using KIOSK.Presentation.Features.Exchange.Resources;
using Localization;

namespace KIOSK.ViewModels.Exchange.Popup
{
    public partial class ExchangePopupTermsViewModel : ObservableObject
    {
        private readonly IPopupService _popup;
        private readonly IExchangeTermsResourceProvider _termsProvider;

        [ObservableProperty]
        private Uri source = new Uri("pack://application:,,,/Assets/Image/Terms/Terms_ko-KR.png");

        public ExchangePopupTermsViewModel(IPopupService popup, IExchangeTermsResourceProvider termsProvider)
        {
            _popup = popup;
            _termsProvider = termsProvider;

            Source = _termsProvider.GetTermsImageUri();
        }

         [RelayCommand]
        private void Close()
        {
            _popup.ClosePopup();
        }

        [RelayCommand]
        public void Accept()
        {
            _popup.ClosePopup();
        }

        [RelayCommand]
        public void Cancel()
        {
            _popup.ClosePopup();
        }
    }
}
