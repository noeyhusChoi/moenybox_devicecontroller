using KIOSK.Infrastructure.API.Gtf;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Infrastructure.Cache;

namespace KIOSK.Infrastructure.API.Cems
{
    public class CemsApiOptionsSetup : IConfigureOptions<CemsApiOptions>
    {
        private readonly DatabaseCache _cache;

        public CemsApiOptionsSetup(DatabaseCache cache)
        {
            _cache = cache;
        }

        public void Configure(CemsApiOptions options)
        {
            var cfg = _cache.ApiConfigList.FirstOrDefault(x => x.ServerName == "CEMS");

            options.BaseUrl = cfg.ServerUrl;
            options.ApiKey = cfg.ServerKey;
            options.TimeoutSeconds = cfg.TimeoutSeconds;
        }
    }
}
