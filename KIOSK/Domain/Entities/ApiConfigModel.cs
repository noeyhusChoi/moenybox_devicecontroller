using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Domain.Entities
{
    public class ApiConfigModel
    {
        public string ServerName { get; set; }

        public string ServerUrl { get; set; }

        public string ServerKey { get; set; }

        public int TimeoutSeconds { get; set; }
    }
}
