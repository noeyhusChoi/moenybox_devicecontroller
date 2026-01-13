using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace KIOSK.Infrastructure.API.Cems
{
    public class CemsApiOptionsSetup : IConfigureOptions<CemsApiOptions>
    {
        private readonly IMemoryCache _cache;

        public CemsApiOptionsSetup(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void Configure(CemsApiOptions options)
        {
            var list = _cache.Get<IReadOnlyList<ApiConfigModel>>(DatabaseCacheKeys.ApiConfigList)
                ?? Array.Empty<ApiConfigModel>();
            var cfg = list.FirstOrDefault(x => x.ServerName == "CEMS");
            if (cfg is null)
                return;

            options.BaseUrl = cfg.ServerUrl;
            options.ApiKey = cfg.ServerKey;
            options.TimeoutSeconds = cfg.TimeoutSeconds;
        }
    }
}
