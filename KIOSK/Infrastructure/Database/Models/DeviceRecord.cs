using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class DeviceRecord
    {
        [Column("DEVICE_ID")]
        public string Id { get; set; } = string.Empty;

        [Column("DEVICE_NAME")]
        public string Name { get; set; } = string.Empty;

        [Column("VENDOR")]
        public string Vendor { get; set; } = string.Empty;

        [Column("MODEL")]
        public string Model { get; set; } = string.Empty;

        [Column("DRIVER_TYPE")]
        public string DriverType { get; set; } = string.Empty;

        [Column("DEVICE_TYPE")]
        public string DeviceType { get; set; } = string.Empty;

        [Column("COMM_TYPE")]
        public string CommType { get; set; } = string.Empty;

        [Column("COMM_PORT")]
        public string CommPort { get; set; } = string.Empty;

        [Column("COMM_PARAMS")]
        public string CommParam { get; set; } = string.Empty;

        [Column("POLLING_MS")]
        public int PollingMs { get; set; }
    }
}
