using KIOSK.Infrastructure.Database.Common;

namespace KIOSK.Infrastructure.Database.Models
{
    public class LocaleInfoRecord
    {
        [Column("CURRENCY_CODE")]
        public string CurrencyCode { get; set; } = string.Empty;

        [Column("LANGUAGE_CODE")]
        public string LanguageCode { get; set; } = string.Empty;

        [Column("COUNTRY_CODE")]
        public string CountryCode { get; set; } = string.Empty;

        [Column("CULTURE_CODE")]
        public string CultureCode { get; set; } = string.Empty;

        [Column("LANGUAGE_NAME")]
        public string LanguageName { get; set; } = string.Empty;

        [Column("LANGUAGE_NAME_KO")]
        public string LanguageNameKo { get; set; } = string.Empty;

        [Column("LANGUAGE_NAME_EN")]
        public string LanguageNameEn { get; set; } = string.Empty;

        [Column("COUNTRY_NAME_KO")]
        public string CountryNameKo { get; set; } = string.Empty;

        [Column("COUNTRY_NAME_EN")]
        public string CountryNameEn { get; set; } = string.Empty;
    }
}
