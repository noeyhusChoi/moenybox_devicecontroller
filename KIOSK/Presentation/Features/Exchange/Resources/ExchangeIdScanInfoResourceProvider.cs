using System;
using System.Globalization;
using System.IO;
using Localization;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeIdScanInfoResourceProvider : IExchangeIdScanInfoResourceProvider
    {
        private readonly ILocalizationService _localizationService;

        public ExchangeIdScanInfoResourceProvider(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public ExchangeIdScanInfoAssets GetAssets()
        {
            var culture = _localizationService.CurrentCulture?.TwoLetterISOLanguageName
                          ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var useIdCard = culture == "ko";

            var imageUri = useIdCard
                ? new Uri("pack://application:,,,/Assets/Image/IDScan_ID.png", UriKind.Absolute)
                : new Uri("pack://application:,,,/Assets/Image/IDScan_Passport.png", UriKind.Absolute);

            var videoFile = useIdCard ? "IDScan_ID.mp4" : "IDScan_Passport.mp4";
            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Video", videoFile);

            return new ExchangeIdScanInfoAssets(imageUri, videoPath);
        }
    }
}
