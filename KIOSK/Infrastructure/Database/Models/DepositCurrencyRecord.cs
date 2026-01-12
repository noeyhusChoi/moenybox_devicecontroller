using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class DepositCurrencyRecord
    {
        [Column("CURRENCY_CODE")]
        public string CurrencyCode { get; set; } = string.Empty;

        [Column("VALUE")]
        public decimal Denomination { get; set; }

        [Column("ATTRIBUTE_CODE")]
        public string AttributeCode { get; set; } = string.Empty;
    }
}
