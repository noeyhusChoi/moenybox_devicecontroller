using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class ReceiptRecord
    {
        [Column("LOCALE")]
        public string Locale { get; set; } = string.Empty;

        [Column("KEY")]
        public string Key { get; set; } = string.Empty;

        [Column("VALUE")]
        public string Value { get; set; } = string.Empty;
    }
}
