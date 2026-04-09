using Kiosk.Infrastructure.Cache;
using Kiosk.Infrastructure.Database.Models;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Kiosk.Application.Services.Localization
{
    public sealed class LocaleInfoProvider : ILocaleInfoProvider
    {
        private readonly IMemoryCache _cache;

        public LocaleInfoProvider(IMemoryCache cache)
        {
            _cache = cache;
        }

        public IReadOnlyList<LocaleInfoModel> LocaleInfoList =>
            _cache.Get<IReadOnlyList<LocaleInfoModel>>(DatabaseCacheKeys.LocaleInfoList)
            ?? Array.Empty<LocaleInfoModel>();
    }
}
