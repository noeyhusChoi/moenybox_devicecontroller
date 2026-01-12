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
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Vendor { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string DriverType { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string CommType { get; set; } = string.Empty;

        public string CommPort { get; set; } = string.Empty;

        public string CommParam { get; set; } = string.Empty;

        public int PollingMs { get; set; }
    }
}
