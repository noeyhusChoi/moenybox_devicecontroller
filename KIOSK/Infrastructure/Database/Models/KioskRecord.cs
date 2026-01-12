using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class KioskRecord
    {
        [Column("KIOSK_ID")]
        public string Id { get; set; } = string.Empty;

        [Column("KIOSK_PID")]
        public string Pid { get; set; } = string.Empty;
    }
}
