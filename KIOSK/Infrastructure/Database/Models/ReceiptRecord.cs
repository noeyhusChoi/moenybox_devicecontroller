using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class ReceiptRecord
    {
        [Column("LOCALE")]
        public string Locale { get; set; }

        [Column("KEY")]
        public string Key { get; set; }

        [Column("VALUE")]
        public string Value { get; set; }
    }
}
