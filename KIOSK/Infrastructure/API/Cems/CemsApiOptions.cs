using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.API.Cems
{
    public sealed class CemsApiOptions
    {
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 15;
    }

    public enum CemsApiCmd { C010, C011, C020, C030, C040, C060, C070, C090 }
}