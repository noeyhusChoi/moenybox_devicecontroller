using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace KIOSK.Domain.Entities
{
    // 장비
    public class DeviceModel
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string CommType { get; set; }

        public string CommPort { get; set; }

        public string CommParam { get; set; }
    }
}
