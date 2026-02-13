using System;
using System.Globalization;
using System.IO;
using KIOSK.Application.Services.Localization;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeTermsResourceProvider : IExchangeTermsResourceProvider
    {
        private readonly ILocalizationService _localizationService;

        public ExchangeTermsResourceProvider(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public Uri GetTermsImageUri()
        {
            var culture = _localizationService.CurrentCulture?.Name ?? CultureInfo.CurrentUICulture.Name;
            var fileName = $"Terms_{culture}.png";
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var termsDir = Path.Combine(baseDir, "Assets", "Image", "Terms");
            var fallbackFile = "Terms_ko-KR.png";

            if (!File.Exists(Path.Combine(termsDir, fileName)))
                fileName = fallbackFile;

            var uri = $"pack://application:,,,/Assets/Image/Terms/{fileName}";
            return new Uri(uri, UriKind.Absolute);
        }
    }
}
