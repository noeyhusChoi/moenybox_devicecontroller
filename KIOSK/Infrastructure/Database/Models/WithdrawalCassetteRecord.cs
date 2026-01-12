using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class WithdrawalCassetteRecord
    {
        [Column("DEVICE_ID")]
        public string DeviceID { get; set; } = string.Empty;

        [Column("DEVICE_NAME")]
        public string DeviceName { get; set; } = string.Empty;

        [Column("SLOT")]
        public int Slot { get; set; }

        [Column("CURRENCY_CODE")]
        public string CurrencyCode { get; set; } = string.Empty;

        [Column("DENOMINATION")]
        public decimal Denomination { get; set; }

        [Column("CAPACITY")]
        public int Capacity { get; set; }

        [Column("CURRENT_COUNT")]
        public int Count { get; set; }
    }
}
