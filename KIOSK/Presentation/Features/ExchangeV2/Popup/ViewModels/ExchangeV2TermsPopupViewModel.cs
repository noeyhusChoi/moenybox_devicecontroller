using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Features.Exchange.Resources;
using KIOSK.Presentation.Navigation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Features.ExchangeV2.Popup.ViewModels
{
    public partial class ExchangeV2TermsPopupViewModel : ObservableObject
    {
        private readonly IPopupService _popup;
        private readonly IExchangeTermsResourceProvider _termsProvider;

        [ObservableProperty]
        private Uri source = new Uri("pack://application:,,,/Assets/Image/Terms/Terms_ko-KR.png");

        public ExchangeV2TermsPopupViewModel(IPopupService popup, IExchangeTermsResourceProvider termsProvider)
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