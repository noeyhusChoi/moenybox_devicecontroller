
namespace KIOSK.Domain.Entities
{
    public class LocaleInfoModel
    {
        public string CurrencyCode { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;

        public string CultureCode { get; set; } = string.Empty;

        public string LanguageName { get; set; } = string.Empty;

        public string LanguageNameKo { get; set; } = string.Empty;

        public string LanguageNameEn { get; set; } = string.Empty;
        
        public string CountryNameKo { get; set; } = string.Empty;
        
        public string CountryNameEn { get; set; } = string.Empty;
    }
}
