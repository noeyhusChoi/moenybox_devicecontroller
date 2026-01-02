using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class DeviceRecord
    {
        [Column("DEVICE_ID")]
        public string Id { get; set; }

        [Column("DEVICE_TYPE")]
        public string Type { get; set; }

        [Column("COMM_TYPE")]
        public string CommType { get; set; }

        [Column("COMM_PORT")]
        public string CommPort { get; set; }

        [Column("COMM_PARAMS")]
        public string CommParam { get; set; }
    }
}
