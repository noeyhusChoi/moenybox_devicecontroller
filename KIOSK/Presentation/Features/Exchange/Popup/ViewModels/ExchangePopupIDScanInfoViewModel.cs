using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Features.Exchange.Resources;
using KIOSK.Infrastructure.Common.Utils;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using KIOSK.Presentation.Navigation.Services;

namespace KIOSK.Presentation.Features.Exchange.Popup.ViewModels
{
    public partial class ExchangePopupIDScanInfoViewModel : ObservableObject
    {
        private readonly IPopupService _popup;
        private readonly IExchangeIdScanInfoResourceProvider _infoProvider;
        [ObservableProperty]
        private BitmapImage imgPath;

        [ObservableProperty]
        private string videoPath;

        public ExchangePopupIDScanInfoViewModel(IPopupService popup, IExchangeIdScanInfoResourceProvider infoProvider)
        {
            _popup = popup;
            _infoProvider = infoProvider;

            var assets = _infoProvider.GetAssets();
            ImgPath = BitmapSafe.LoadBitmap(assets.ImageUri);
            VideoPath = assets.VideoPath;
        }

        [RelayCommand]
        private async Task Close()
        {
            VideoPath = null;

            _popup.ClosePopup();
        }

        [RelayCommand]
        public void Accept()
        {
            VideoPath = null;

            _popup.ClosePopup();
        }

        [RelayCommand]
        public void Cancel()
        {
            VideoPath = null;

            _popup.ClosePopup();
        }
    }
}