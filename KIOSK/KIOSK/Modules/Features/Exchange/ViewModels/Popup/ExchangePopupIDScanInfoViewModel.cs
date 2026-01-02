using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Infrastructure.UI;
using KIOSK.Infrastructure.UI.Navigation;
using KIOSK.Services;
using KIOSK.Utils;
using Localization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KIOSK.Infrastructure.Media;

namespace KIOSK.ViewModels.Exchange.Popup
{
    public partial class ExchangePopupIDScanInfoViewModel : ObservableObject
    {
        private readonly IPopupService _popup;
        private readonly ILocalizationService _localizationService;
        private readonly IVideoPlayService _videoPlayService;

        [ObservableProperty]
        private BitmapImage imgPath;

        [ObservableProperty]
        private Uri videoPath;

        [ObservableProperty]
        private Brush? backgroundBrush;

        public ExchangePopupIDScanInfoViewModel(IPopupService popup, ILocalizationService localization, IVideoPlayService videoPlay)
        {
            // TODO: 1. 언어에 따른 파일 변환 (1차)
            //       2. 언어 선택시 전환 로직 추가 개발 (2차)

            _popup = popup;
            _localizationService = localization;
            _videoPlayService = videoPlay;

            // 한국어 선택 시 ID카드, 그 외 여권
            if (_localizationService.CurrentCulture.TwoLetterISOLanguageName == "ko")
            {
                ImgPath = BitmapSafe.LoadBitmap(new Uri("pack://application:,,,/Assets/Image/IDScan_ID.png", UriKind.Absolute));
                VideoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "IDScan_ID.mp4"), UriKind.Absolute);
            }
            else
            {
                ImgPath = BitmapSafe.LoadBitmap(new Uri("pack://application:,,,/Assets/Image/IDScan_Passport.png", UriKind.Absolute));
                VideoPath = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", "IDScan_Passport.mp4"), UriKind.Absolute);
            }

            BackgroundBrush = _videoPlayService.BackgroundBrush;
            _videoPlayService.SetSource(VideoPath, loop: true, mute: true, autoPlay: true);
        }

        [RelayCommand]
        private async Task Close()
        {
            VideoPath = null;
            BackgroundBrush = null;
            _videoPlayService.Stop();

            _popup.CloseLocal();
        }

        [RelayCommand]
        public void Accept()
        {
            VideoPath = null;
            BackgroundBrush = null;
            _videoPlayService.Stop();

            _popup.CloseLocal();
        }

        [RelayCommand]
        public void Cancel()
        {
            VideoPath = null;
            BackgroundBrush = null;
            _videoPlayService.Stop();

            _popup.CloseLocal();
        }
    }
}
