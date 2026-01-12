using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;

namespace KIOSK.Application.Services.DataBase
{
    public class WithdrawalCassetteServiceV2
    {
        public readonly WithdrawalCassetteRepository _repo;
        private readonly DatabaseCache _cache;

        public WithdrawalCassetteServiceV2(WithdrawalCassetteRepository repo, DatabaseCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<IReadOnlyList<WithdrawalCassetteModel>> GetSlotsAsync()
        {
            if (_cache.WithdrawalCassetteList == null || _cache.WithdrawalCassetteList.Count == 0)
            {
                _cache.WithdrawalCassetteList = await _repo.LoadAllAsync();
            }
            return _cache.WithdrawalCassetteList;
        }

        public async Task SaveAsync(IReadOnlyList<WithdrawalCassetteModel> edited)
        {
            // DB Update
            await _repo.UpdateAsync(edited);

            // Refresh cache
            _cache.WithdrawalCassetteList = await _repo.LoadAllAsync();
        }
    }
}