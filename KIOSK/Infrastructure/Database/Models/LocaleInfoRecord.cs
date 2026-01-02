using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class LocaleInfoRecord
    {
        [Column("CURRENCY_CODE")]
        public string CurrencyCode { get; set; }

        [Column("LANGUAGE_CODE")]
        public string LanguageCode { get; set; }

        [Column("COUNTRY_CODE")]
        public string CountryCode { get; set; }

        [Column("CULTURE_CODE")]
        public string CultureCode { get; set; }

        [Column("LANGUAGE_NAME")]
        public string LanguageName { get; set; }

        [Column("LANGUAGE_NAME_KO")]
        public string LanguageNameKo { get; set; }

        [Column("LANGUAGE_NAME_EN")]
        public string LanguageNameEn { get; set; }

        [Column("COUNTRY_NAME_KO")]
        public string CountryNameKo { get; set; }

        [Column("COUNTRY_NAME_EN")]
        public string CountryNameEn { get; set; }
    }
}
