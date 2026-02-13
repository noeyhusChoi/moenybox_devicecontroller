using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Application.Services.Localization;

namespace KIOSK.Application.Services.Exchange
{
    public sealed class ExchangeSelectLanguageUseCase : IExchangeSelectLanguageUseCase
    {
        private readonly ILocalizationService _localizationService;

        public ExchangeSelectLanguageUseCase(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public Task SelectAsync(string? selection, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(selection))
                return Task.CompletedTask;

            var culture = selection switch
            {
                "en-US" => new CultureInfo("en-US"),
                "zh-CN" => new CultureInfo("zh-CN"),
                "zh-TW" => new CultureInfo("zh-TW"),
                "ja-JP" => new CultureInfo("ja-JP"),
                "ko-KR" => new CultureInfo("ko-KR"),
                _ => new CultureInfo("ko-KR")
            };

            _localizationService.SetCulture(culture);

            return Task.CompletedTask;
        }
    }
}
