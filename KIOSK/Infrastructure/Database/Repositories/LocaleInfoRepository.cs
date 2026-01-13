using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Ef.Entities;
using KIOSK.Infrastructure.Database.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Database.Repositories
{
    internal class LocaleInfoRepository : IReadRepository<LocaleInfoModel>
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;

        public LocaleInfoRepository(IDbContextFactory<KioskDbContext> contextFactory)
            => _contextFactory = contextFactory;

        public async Task<IReadOnlyList<LocaleInfoModel>> LoadAllAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var records = await context.LocaleInfos
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return records.Select(Map).ToList();
        }

        private static LocaleInfoModel Map(LocaleInfoEntity record)
            => new LocaleInfoModel
            {
                CurrencyCode = string.Empty,
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
