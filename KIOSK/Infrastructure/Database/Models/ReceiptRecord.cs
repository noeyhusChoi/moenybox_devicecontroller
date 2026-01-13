using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class ReceiptRecord
    {
        [Column("KIOSK_ID")]
        public string KioskId { get; set; } = string.Empty;

        [Column("INFO_LOCALE")]
        public string Locale { get; set; } = string.Empty;

        [Column("INFO_KEY")]
        public string Key { get; set; } = string.Empty;

        [Column("INFO_VALUE")]
        public string Value { get; set; } = string.Empty;
    }
}
