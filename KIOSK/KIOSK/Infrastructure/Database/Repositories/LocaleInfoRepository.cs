using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Interface;
using KIOSK.Infrastructure.Database.Models;
using System.Linq;

namespace KIOSK.Infrastructure.Database.Repositories
{
    internal class LocaleInfoRepository : RepositoryBase, IReadRepository<LocaleInfoModel>
    {
        public LocaleInfoRepository(IDatabaseService db) : base(db)
        {
        }

        public async Task<IReadOnlyList<LocaleInfoModel>> LoadAllAsync(CancellationToken ct = default)
        {
            var records = await QueryAsync<LocaleInfoRecord>("sp_get_locale_info", null, ct);
            return records.Select(Map).ToList();
        }

        private static LocaleInfoModel Map(LocaleInfoRecord record)
            => new LocaleInfoModel
            {
                CurrencyCode = record.CurrencyCode,
                LanguageCode = record.LanguageCode,
                CountryCode = record.CountryCode,
                CultureCode = record.CultureCode,
                LanguageName = record.LanguageName,
                LanguageNameKo = record.LanguageNameKo,
                LanguageNameEn = record.LanguageNameEn,
                CountryNameKo = record.CountryNameKo,
                CountryNameEn = record.CountryNameEn
            };
    }
}
