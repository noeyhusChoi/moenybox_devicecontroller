using KIOSK.Domain.Entities;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KIOSK.Application.Services.DataBase
{
    public class WithdrawalCassetteServiceV2
    {
        public readonly WithdrawalCassetteRepository _repo;
        private readonly IMemoryCache _cache;

        public WithdrawalCassetteServiceV2(WithdrawalCassetteRepository repo, IMemoryCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<IReadOnlyList<WithdrawalCassetteModel>> GetSlotsAsync()
        {
            var list = _cache.Get<IReadOnlyList<WithdrawalCassetteModel>>(DatabaseCacheKeys.WithdrawalCassetteList);
            if (list is null || list.Count == 0)
            {
                list = await _repo.LoadAllAsync().ConfigureAwait(false);
                _cache.Set(DatabaseCacheKeys.WithdrawalCassetteList, list);
            }

            return list;
        }

        public async Task SaveAsync(IReadOnlyList<WithdrawalCassetteModel> edited)
        {
            // DB Update
            await _repo.UpdateAsync(edited).ConfigureAwait(false);

            // Refresh cache
            var list = await _repo.LoadAllAsync().ConfigureAwait(false);
            _cache.Set(DatabaseCacheKeys.WithdrawalCassetteList, list);
        }
    }
}
