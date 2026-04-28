using Kiosk.Infrastructure.Cache;
using Kiosk.Infrastructure.Database.Ef;
using Kiosk.Infrastructure.Database.Ef.Entities;
using Kiosk.Infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kiosk.Application.Services
{
    public readonly record struct WithdrawalCassette(string DeviceID, int Slot, string CurrencyCode, decimal Denomination, int Capacity, int Count);

    public sealed class WithdrawalCassetteService
    {
        private readonly IDbContextFactory<KioskDbContext> _contextFactory;
        private readonly IMemoryCache _cache;
        private volatile HashSet<WithdrawalCassette> _withdrawalCassettes = new();

        public WithdrawalCassetteService(IDbContextFactory<KioskDbContext> contextFactory, IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _cache = cache;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            await LoadAsync(ct).ConfigureAwait(false);
        }

        public decimal GetTotalAmount(string currency)
        {
            return _withdrawalCassettes
                .Where(x => x.CurrencyCode == currency)
                .Sum(x => x.Denomination * x.Count);
        }

        public HashSet<WithdrawalCassette> Get() => _withdrawalCassettes;

        private async Task LoadAsync(CancellationToken ct)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                var records = await context.WithdrawalCassettes
                    .AsNoTracking()
                    .Where(x => x.IsValid)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                if (records.Count == 0)
                    return;

                var next = new HashSet<WithdrawalCassette>(records.Count);
                foreach (var record in records)
                {
                    next.Add(new WithdrawalCassette(
                        record.DeviceID,
                        record.Slot,
                        record.CurrencyCode,
                        record.Denomination,
                        record.Capacity,
                        record.Count));
                }

                _withdrawalCassettes = next;
            }
            catch (Exception)
            {
            }
        }

        public async Task WithdrawalAsync(IEnumerable<(string deviceId, string currency_code, int slot, decimal denomination, int succeeded_count)> results, CancellationToken ct)
        {
            try
            {
                var resultList = results.ToList();
                if (resultList.Count == 0)
                    return;

                await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                var kioskId = ResolveKioskId();
                var deviceIds = resultList.Select(x => x.deviceId).Distinct().ToList();
                var slots = resultList.Select(x => x.slot).Distinct().ToList();

                var query = context.WithdrawalCassettes
                    .Where(x => deviceIds.Contains(x.DeviceID) && slots.Contains(x.Slot));

                if (!string.IsNullOrWhiteSpace(kioskId))
                    query = query.Where(x => x.KioskId == kioskId);

                var rows = await query.ToListAsync(ct).ConfigureAwait(false);
                var map = rows.ToDictionary(
                    x => (x.DeviceID, x.CurrencyCode, x.Slot, x.Denomination),
                    x => x);

                foreach (var result in resultList)
                {
                    var key = (result.deviceId, result.currency_code, result.slot, result.denomination);
                    if (!map.TryGetValue(key, out var row))
                        continue;

                    row.Count = Math.Max(0, row.Count - result.succeeded_count);
                    row.UpdatedAt = DateTime.UtcNow;
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                await LoadAsync(ct).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        public async Task ResultAsync(string json, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            var transactionId = TryResolveTransactionId(json) ?? Guid.NewGuid().ToString("N");

            var row = await context.TransactionOutboxes
                .SingleOrDefaultAsync(x => x.TransactionId == transactionId, ct)
                .ConfigureAwait(false);

            if (row is null)
            {
                context.TransactionOutboxes.Add(new TransactionOutboxEntity
                {
                    KioskId = ResolveKioskId() ?? string.Empty,
                    TransactionId = transactionId,
                    MessageType = "TX_RESULT",
                    PayloadJson = json,
                    Status = "PENDING",
                    RetryCount = 0,
                    NextRetryAt = now,
                    CreatedAt = now
                });
            }
            else
            {
                row.PayloadJson = json;
                row.MessageType = "TX_RESULT";
                row.Status = "PENDING";
                row.LastTriedAt = null;
                row.NextRetryAt = now;
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private string? ResolveKioskId()
        {
            var kiosks = _cache.Get<IReadOnlyList<KioskModel>>(DatabaseCacheKeys.Kiosk);
            return kiosks?.FirstOrDefault()?.Id;
        }

        private static string? TryResolveTransactionId(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                foreach (var propertyName in new[] { "TransactionId", "transactionId", "TransactionID", "transactionID" })
                {
                    if (document.RootElement.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
                        return value.GetString();
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }
}
