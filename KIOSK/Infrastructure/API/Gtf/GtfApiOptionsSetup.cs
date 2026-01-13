using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace KIOSK.Infrastructure.API.Gtf
{

    public class GtfApiOptionsSetup : IConfigureOptions<GtfApiOptions>
    {
        private readonly IMemoryCache _cache;

        public GtfApiOptionsSetup(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void Configure(GtfApiOptions options)
        {
            var list = _cache.Get<IReadOnlyList<ApiConfigModel>>(DatabaseCacheKeys.ApiConfigList)
                ?? Array.Empty<ApiConfigModel>();
            var cfg = list.FirstOrDefault(x => x.ServerName == "GTF");
            if (cfg is null)
                return;

            options.BaseUrl = cfg.ServerUrl;
            options.TimeoutSeconds = cfg.TimeoutSeconds;
        }
    }
}
