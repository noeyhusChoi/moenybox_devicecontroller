using System.IO;
using System.Text.Json;
using Kiosk.Application.Abstractions;
using Kiosk.Infrastructure.Database.Ef;
using Kiosk.Infrastructure.Database.Ef.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Initialization;

public sealed class ReferenceDataSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IDbContextFactory<KioskDbContext> _contextFactory;
    private readonly ILoggingService _logging;

    public ReferenceDataSyncService(
        IDbContextFactory<KioskDbContext> contextFactory,
        ILoggingService logging)
    {
        _contextFactory = contextFactory;
        _logging = logging;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        var referenceDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Reference");
        if (!Directory.Exists(referenceDirectory))
        {
            _logging.Warn($"[ReferenceDataSync] Reference data directory not found: {referenceDirectory}");
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var currencies = await LoadAsync<CurrencySeedRecord>(referenceDirectory, "currency.json", ct).ConfigureAwait(false);
        var depositDenominations = await LoadAsync<DepositDenominationSeedRecord>(referenceDirectory, "deposit_denom.json", ct).ConfigureAwait(false);
        var depositCurrencyAttributes = await LoadAsync<DepositCurrencyAttributeSeedRecord>(referenceDirectory, "deposit_denom_attribute.json", ct).ConfigureAwait(false);

        if (currencies is null && depositDenominations is null && depositCurrencyAttributes is null)
        {
            _logging.Warn("[ReferenceDataSync] No reference data files were loaded.");
            return;
        }

        if (currencies is not null)
        {
            await SyncCurrenciesAsync(context, currencies, ct).ConfigureAwait(false);
        }

        if (depositDenominations is not null)
        {
            await SyncDepositDenominationsAsync(context, depositDenominations, ct).ConfigureAwait(false);
        }

        if (depositCurrencyAttributes is not null)
        {
            await SyncDepositCurrencyAttributesAsync(context, depositCurrencyAttributes, ct).ConfigureAwait(false);
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SyncCurrenciesAsync(KioskDbContext context, IReadOnlyList<CurrencySeedRecord> records, CancellationToken ct)
    {
        var existing = await context.Currencies.ToListAsync(ct).ConfigureAwait(false);
        var existingByKey = existing.ToDictionary(x => CurrencyKey(x.KioskId, x.CurrencyCode), StringComparer.OrdinalIgnoreCase);
        var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var key = CurrencyKey(record.KioskId, record.CurrencyCode);
            incomingKeys.Add(key);

            if (!existingByKey.TryGetValue(key, out var entity))
            {
                context.Currencies.Add(new CurrencyEntity
                {
                    KioskId = record.KioskId,
                    CultureCode = record.CultureCode,
                    CurrencyCode = record.CurrencyCode,
                    CurrencyDecimal = record.CurrencyDecimal,
                    CurrencySymbol = record.CurrencySymbol,
                    IsValid = record.IsValid,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                });
                continue;
            }

            entity.CultureCode = record.CultureCode;
            entity.CurrencyDecimal = record.CurrencyDecimal;
            entity.CurrencySymbol = record.CurrencySymbol;
            entity.IsValid = record.IsValid;
            entity.CreatedAt = record.CreatedAt;
            entity.UpdatedAt = record.UpdatedAt;
        }

        foreach (var entity in existing.Where(x => !incomingKeys.Contains(CurrencyKey(x.KioskId, x.CurrencyCode)) && x.IsValid))
        {
            entity.IsValid = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task SyncDepositDenominationsAsync(KioskDbContext context, IReadOnlyList<DepositDenominationSeedRecord> records, CancellationToken ct)
    {
        var existing = await context.DepositDenominations.ToListAsync(ct).ConfigureAwait(false);
        var existingByKey = existing.ToDictionary(x => DepositDenominationKey(x.KioskId, x.CurrencyCode, x.Denomination), StringComparer.OrdinalIgnoreCase);
        var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var key = DepositDenominationKey(record.KioskId, record.CurrencyCode, record.Denomination);
            incomingKeys.Add(key);

            if (!existingByKey.TryGetValue(key, out var entity))
            {
                context.DepositDenominations.Add(new DepositDenominationEntity
                {
                    KioskId = record.KioskId,
                    CurrencyCode = record.CurrencyCode,
                    Denomination = record.Denomination,
                    IsValid = record.IsValid,
                    UpdatedBy = record.UpdatedBy,
                    UpdatedAt = record.UpdatedAt,
                });
                continue;
            }

            entity.IsValid = record.IsValid;
            entity.UpdatedBy = record.UpdatedBy;
            entity.UpdatedAt = record.UpdatedAt;
        }

        foreach (var entity in existing.Where(x => !incomingKeys.Contains(DepositDenominationKey(x.KioskId, x.CurrencyCode, x.Denomination)) && x.IsValid))
        {
            entity.IsValid = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task SyncDepositCurrencyAttributesAsync(KioskDbContext context, IReadOnlyList<DepositCurrencyAttributeSeedRecord> records, CancellationToken ct)
    {
        var existing = await context.DepositCurrencies.ToListAsync(ct).ConfigureAwait(false);
        var existingByKey = existing.ToDictionary(
            x => DepositCurrencyAttributeKey(x.KioskId, x.CurrencyCode, x.Denomination, x.AttributeCode),
            StringComparer.OrdinalIgnoreCase);
        var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var key = DepositCurrencyAttributeKey(record.KioskId, record.CurrencyCode, record.Denomination, record.AttributeCode);
            incomingKeys.Add(key);

            if (!existingByKey.TryGetValue(key, out var entity))
            {
                context.DepositCurrencies.Add(new DepositCurrencyEntity
                {
                    Id = record.Id,
                    KioskId = record.KioskId,
                    CurrencyCode = record.CurrencyCode,
                    Denomination = record.Denomination,
                    AttributeCode = record.AttributeCode,
                    IsValid = record.IsValid,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                });
                continue;
            }

            entity.CurrencyCode = record.CurrencyCode;
            entity.Denomination = record.Denomination;
            entity.AttributeCode = record.AttributeCode;
            entity.IsValid = record.IsValid;
            entity.CreatedAt = record.CreatedAt;
            entity.UpdatedAt = record.UpdatedAt;
        }

        foreach (var entity in existing.Where(x => !incomingKeys.Contains(DepositCurrencyAttributeKey(x.KioskId, x.CurrencyCode, x.Denomination, x.AttributeCode)) && x.IsValid))
        {
            entity.IsValid = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<IReadOnlyList<T>?> LoadAsync<T>(string directory, string fileName, CancellationToken ct)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
        {
            _logging.Warn($"[ReferenceDataSync] Reference data file not found: {path}");
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, ct).ConfigureAwait(false);
    }

    private static string CurrencyKey(string kioskId, string currencyCode)
        => $"{kioskId}::{currencyCode}".ToUpperInvariant();

    private static string DepositDenominationKey(string kioskId, string currencyCode, decimal denomination)
        => $"{kioskId}::{currencyCode}::{denomination:0.##}".ToUpperInvariant();

    private static string DepositCurrencyAttributeKey(string kioskId, string currencyCode, decimal denomination, string attributeCode)
        => $"{kioskId}::{currencyCode}::{denomination:0.##}::{attributeCode}".ToUpperInvariant();

    public sealed class CurrencySeedRecord
    {
        public string KioskId { get; set; } = string.Empty;
        public string CultureCode { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public int CurrencyDecimal { get; set; }
        public string CurrencySymbol { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class DepositDenominationSeedRecord
    {
        public string KioskId { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal Denomination { get; set; }
        public bool IsValid { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class DepositCurrencyAttributeSeedRecord
    {
        public long Id { get; set; }
        public string KioskId { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal Denomination { get; set; }
        public string AttributeCode { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
