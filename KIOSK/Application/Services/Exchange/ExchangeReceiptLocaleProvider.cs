using Localization;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeReceiptLocaleProvider : IExchangeReceiptLocaleProvider
    {
        private readonly ILocalizationService _localizationService;

        public ExchangeReceiptLocaleProvider(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public string GetLocale()
        {
            var cultureName = _localizationService.CurrentCulture.Name;
            return cultureName.StartsWith("ko") ? "ko-KR" : "en-US";
        }
    }
}
