using System.Globalization;
using Localization;
using System.Threading;
using System.Threading.Tasks;

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
                "1" => new CultureInfo("en-US"),
                "2" => new CultureInfo("zh-CN"),
                "3" => new CultureInfo("zh-TW"),
                "4" => new CultureInfo("ja-JP"),
                "5" => new CultureInfo("ko-KR"),
                _ => new CultureInfo("ko-KR")
            };

            _localizationService.SetCulture(culture);

            return Task.CompletedTask;
        }
    }
}
