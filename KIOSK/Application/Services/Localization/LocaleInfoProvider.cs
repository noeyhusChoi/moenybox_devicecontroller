using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Cache;

namespace KIOSK.Application.Services.Localization
{
    public sealed class LocaleInfoProvider : ILocaleInfoProvider
    {
        private readonly DatabaseCache _cache;

        public LocaleInfoProvider(DatabaseCache cache)
        {
            _cache = cache;
        }

        public IReadOnlyList<LocaleInfoModel> LocaleInfoList => _cache.LocaleInfoList;
    }
}
