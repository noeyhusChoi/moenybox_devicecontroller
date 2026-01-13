using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Domain.Entities
{
    public class ApiConfigModel
    {
        public string KioskId { get; set; } = string.Empty;

        public string ServerName { get; set; } = string.Empty;

        public string ServerUrl { get; set; } = string.Empty;

        public string ServerKey { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; }

        public bool IsValid { get; set; }
    }
}
