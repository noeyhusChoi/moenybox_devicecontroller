using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Infrastructure.Cache;

namespace KIOSK.Infrastructure.API.Gtf
{

    public class GtfApiOptionsSetup : IConfigureOptions<GtfApiOptions>
    {
        private readonly DatabaseCache _cache;

        public GtfApiOptionsSetup(DatabaseCache cache)
        {
            _cache = cache;
        }

        public void Configure(GtfApiOptions options)
        {
            var cfg = _cache.ApiConfigList.FirstOrDefault(x => x.ServerName == "GTF");

            options.BaseUrl = cfg.ServerUrl;
            options.TimeoutSeconds = cfg.TimeoutSeconds;
        }
    }
}
