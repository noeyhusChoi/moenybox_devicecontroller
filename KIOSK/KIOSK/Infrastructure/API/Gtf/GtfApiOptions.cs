using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.API.Gtf
{
    /// <summary>
    /// 시내환급(새 서버) 전용 옵션
    /// </summary>
    public sealed class GtfApiOptions
    {
        /// <summary>예: https://gtf-api.moneybox.or.kr</summary>
        public string BaseUrl { get; set; } = "https://localhost:7054";

        public int TimeoutSeconds { get; set; } = 15;
    }
}
