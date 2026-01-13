using KIOSK.Infrastructure.Database.Ef.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIOSK.Infrastructure.Database.Ef;

public sealed class KioskDbContext : DbContext
{
    public KioskDbContext(DbContextOptions<KioskDbContext> options)
        : base(options)
    {
    }

    public DbSet<DeviceStatusLogEntity> DeviceStatusLogs => Set<DeviceStatusLogEntity>();
    public DbSet<DeviceCommandLogEntity> DeviceCommandLogs => Set<DeviceCommandLogEntity>();
    public DbSet<DeviceCatalogEntity> DeviceCatalogs => Set<DeviceCatalogEntity>();
    public DbSet<DeviceInstanceEntity> DeviceInstances => Set<DeviceInstanceEntity>();
    public DbSet<DeviceCommEntity> DeviceComms => Set<DeviceCommEntity>();
    public DbSet<ApiConfigEntity> ApiConfigs => Set<ApiConfigEntity>();
    public DbSet<DepositCurrencyEntity> DepositCurrencies => Set<DepositCurrencyEntity>();
    public DbSet<KioskInfoEntity> Kiosks => Set<KioskInfoEntity>();
    public DbSet<ReceiptEntity> Receipts => Set<ReceiptEntity>();
    public DbSet<LocaleInfoEntity> LocaleInfos => Set<LocaleInfoEntity>();
    public DbSet<WithdrawalCassetteEntity> WithdrawalCassettes => Set<WithdrawalCassetteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceStatusLogEntity>(entity =>
        {
            entity.ToTable("device_status_log");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.KioskId).HasColumnName("kiosk_id").HasMaxLength(64);
            entity.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(64);
            entity.Property(x => x.DeviceType).HasColumnName("device_type").HasMaxLength(32);
            entity.Property(x => x.Source).HasColumnName("source").HasMaxLength(16);
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            entity.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(16);
            entity.Property(x => x.Message).HasColumnName("message").HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<DeviceCommandLogEntity>(entity =>
        {
            entity.ToTable("device_command_log");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(64);
            entity.Property(x => x.CommandName).HasColumnName("command_name").HasMaxLength(64);
            entity.Property(x => x.Success).HasColumnName("success");
            entity.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(64);
            entity.Property(x => x.Origin).HasColumnName("origin").HasMaxLength(16);
            entity.Property(x => x.StartedAt).HasColumnName("started_at");
            entity.Property(x => x.FinishedAt).HasColumnName("finished_at");
            entity.Property(x => x.DurationMs).HasColumnName("duration_ms");
            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<DeviceCatalogEntity>(entity =>
        {
            entity.ToTable("device_catalog");
            entity.HasKey(x => x.CatalogId);
            entity.Property(x => x.CatalogId).HasColumnName("catalog_id");
            entity.Property(x => x.Vendor).HasColumnName("vendor").HasMaxLength(64);
            entity.Property(x => x.Model).HasColumnName("model").HasMaxLength(64);
            entity.Property(x => x.DriverType).HasColumnName("driver_type").HasMaxLength(64);
            entity.Property(x => x.DeviceType).HasColumnName("device_type").HasMaxLength(32);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => new { x.Vendor, x.Model, x.DriverType })
                .HasDatabaseName("uq_catalog_vendor_model_driver")
                .IsUnique();
        });

        modelBuilder.Entity<DeviceInstanceEntity>(entity =>
        {
            entity.ToTable("device_instance");
            entity.HasKey(x => x.DeviceId);
            entity.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(64);
            entity.Property(x => x.KioskId).HasColumnName("kiosk_id").HasMaxLength(64);
            entity.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(64);
            entity.Property(x => x.CatalogId).HasColumnName("catalog_id");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => new { x.KioskId, x.DeviceId, x.DeviceName })
                .HasDatabaseName("uq_instance_kiosk_name")
                .IsUnique();
            entity.HasOne(x => x.Catalog)
                .WithMany(x => x.Instances)
                .HasForeignKey(x => x.CatalogId)
                .HasConstraintName("device_instance_ibfk_1");
            entity.HasOne(x => x.Comm)
                .WithOne(x => x.Device)
                .HasForeignKey<DeviceCommEntity>(x => x.DeviceId)
                .HasConstraintName("device_comm_ibfk_1");
        });

        modelBuilder.Entity<DeviceCommEntity>(entity =>
        {
            entity.ToTable("device_comm");
            entity.HasKey(x => x.DeviceId);
            entity.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(64);
            entity.Property(x => x.CommType).HasColumnName("comm_type").HasMaxLength(32);
            entity.Property(x => x.CommPort).HasColumnName("comm_port").HasMaxLength(64);
            entity.Property(x => x.CommParams).HasColumnName("comm_params").HasMaxLength(128);
            entity.Property(x => x.PollingMs).HasColumnName("polling_ms");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ApiConfigEntity>(entity =>
        {
            entity.ToTable("server");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID");
            entity.Property(x => x.KioskId).HasColumnName("KIOSK_ID").HasMaxLength(36);
            entity.Property(x => x.ServerName).HasColumnName("SERVER_NAME").HasMaxLength(64);
            entity.Property(x => x.ServerUrl).HasColumnName("SERVER_URL").HasMaxLength(512);
            entity.Property(x => x.ServerKey).HasColumnName("SERVER_KEY").HasMaxLength(255);
            entity.Property(x => x.TimeoutSeconds).HasColumnName("TIMEOUT_SECONDS");
            entity.Property(x => x.IsValid).HasColumnName("VLD");
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
            entity.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");
        });

        modelBuilder.Entity<DepositCurrencyEntity>(entity =>
        {
            entity.ToTable("deposit_denom_attribute");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.KioskId, x.CurrencyCode, x.Denomination, x.AttributeCode })
                .HasDatabaseName("UQ_DENOM_ATTR")
                .IsUnique();
            entity.Property(x => x.KioskId).HasColumnName("KIOSK_ID").HasMaxLength(36);
            entity.Property(x => x.CurrencyCode).HasColumnName("CURRENCY_CODE").HasMaxLength(3);
            entity.Property(x => x.Denomination).HasColumnName("VALUE");
            entity.Property(x => x.AttributeCode).HasColumnName("ATTRIBUTE_CODE").HasMaxLength(32);
            entity.Property(x => x.IsValid).HasColumnName("VLD");
            entity.Property(x => x.Id).HasColumnName("ID");
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
            entity.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");
        });

        modelBuilder.Entity<KioskInfoEntity>(entity =>
        {
            entity.ToTable("kiosk");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("KIOSK_ID").HasMaxLength(36);
            entity.Property(x => x.Pid).HasColumnName("KIOSK_PID").HasMaxLength(64);
            entity.Property(x => x.IsValid).HasColumnName("VLD");
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
            entity.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");
        });

        modelBuilder.Entity<ReceiptEntity>(entity =>
        {
            entity.ToTable("kiosk_shop");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID");
            entity.Property(x => x.KioskId).HasColumnName("KIOSK_ID").HasMaxLength(36);
            entity.Property(x => x.Locale).HasColumnName("INFO_LOCALE").HasMaxLength(16);
            entity.Property(x => x.Key).HasColumnName("INFO_KEY").HasMaxLength(128);
            entity.Property(x => x.Value).HasColumnName("INFO_VALUE");
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
            entity.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");
            entity.HasIndex(x => x.KioskId)
                .HasDatabaseName("KIOSK_ID");
        });

        modelBuilder.Entity<LocaleInfoEntity>(entity =>
        {
            entity.ToTable("locale_info");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID");
            entity.Property(x => x.LanguageCode).HasColumnName("LANGUAGE_CODE").HasMaxLength(5);
            entity.Property(x => x.CountryCode).HasColumnName("COUNTRY_CODE").HasMaxLength(5);
            entity.Property(x => x.CultureCode).HasColumnName("CULTURE_CODE").HasMaxLength(10);
            entity.Property(x => x.LanguageName).HasColumnName("LANGUAGE_NAME").HasMaxLength(50);
            entity.Property(x => x.LanguageNameKo).HasColumnName("LANGUAGE_NAME_KO").HasMaxLength(50);
            entity.Property(x => x.LanguageNameEn).HasColumnName("LANGUAGE_NAME_EN").HasMaxLength(50);
            entity.Property(x => x.CountryNameKo).HasColumnName("COUNTRY_NAME_KO").HasMaxLength(50);
            entity.Property(x => x.CountryNameEn).HasColumnName("COUNTRY_NAME_EN").HasMaxLength(50);
            entity.HasIndex(x => x.CultureCode)
                .HasDatabaseName("UQ_LOCALE_CODE")
                .IsUnique();
            entity.HasIndex(x => new { x.LanguageNameKo, x.LanguageNameEn, x.CountryNameKo, x.CountryNameEn })
                .HasDatabaseName("UQ_LOCALE_NAMES")
                .IsUnique();
        });

        modelBuilder.Entity<WithdrawalCassetteEntity>(entity =>
        {
            entity.ToTable("cassette");
            entity.HasKey(x => new { x.KioskId, x.DeviceID, x.Slot });
            entity.Property(x => x.KioskId).HasColumnName("KIOSK_ID").HasMaxLength(36);
            entity.Property(x => x.DeviceID).HasColumnName("DEVICE_ID").HasMaxLength(36);
            entity.Property(x => x.CurrencyCode).HasColumnName("CURRENCY_CODE").HasMaxLength(3);
            entity.Property(x => x.Slot).HasColumnName("SLOT");
            entity.Property(x => x.Denomination).HasColumnName("DENOMINATION");
            entity.Property(x => x.Capacity).HasColumnName("CAPACITY");
            entity.Property(x => x.Count).HasColumnName("CURRENT_COUNT");
            entity.Property(x => x.IsValid).HasColumnName("VLD");
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
            entity.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");
        });
    }
}
