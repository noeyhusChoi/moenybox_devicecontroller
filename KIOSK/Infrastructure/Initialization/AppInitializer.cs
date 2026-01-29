using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Management.Devices;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database.Ef;
using KIOSK.Infrastructure.Database.Repositories;
using KIOSK.Infrastructure.Initialization;
using KIOSK.Application.Abstractions;
using KIOSK.Infrastructure.Media;
using KIOSK.Application.Services;
using KIOSK.Domain.Entities;
using Localization;
using Localization.Resx;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;

public class AppInitializer : IAppInitializer
{
    private readonly IDbContextFactory<KioskDbContext> _dbContextFactory;
    private readonly ILocalizationService _localization;            // xaml 기반 다국어 (resx 기반으로 변경 예정)
    private readonly IResxLocalizationService _resxLocalization;    // resx 기반 다국어
    private readonly ILoggingService _logging;
    private readonly IAudioPlayService _audioService;
    private readonly IDeviceManager _deviceManager;
    private readonly IMemoryCache _cache;

    private readonly ApiConfigRepository _apiConfigRepo;
    private readonly DepositCurrencyRepository _depositCurrencyRepo;
    private readonly KioskRepository _kioskRepo;
    private readonly DeviceRepository _deviceRepo;
    private readonly ReceiptRepository _receiptRepo;
    private readonly LocaleInfoRepository _localeInfoRepo;
    private readonly WithdrawalCassetteRepository _withdrawalCassetteRepo;
    private readonly WithdrawalCassetteService _withdrawalCassetteService;

    public bool IsInitialized { get; private set; }

    public event Action<string>? ProgressChanged;

    public AppInitializer(IServiceProvider sp)
    {
        _dbContextFactory = sp.GetRequiredService<IDbContextFactory<KioskDbContext>>();
        _localization = sp.GetRequiredService<ILocalizationService>();
        _resxLocalization = sp.GetRequiredService<IResxLocalizationService>();
        _logging = sp.GetRequiredService<ILoggingService>();
        _audioService = sp.GetRequiredService<IAudioPlayService>();
        _deviceManager = sp.GetRequiredService<IDeviceManager>();

        _cache = sp.GetRequiredService<IMemoryCache>();
        _apiConfigRepo = sp.GetRequiredService<ApiConfigRepository>();
        _depositCurrencyRepo = sp.GetRequiredService<DepositCurrencyRepository>();
        _kioskRepo = sp.GetRequiredService<KioskRepository>();
        _deviceRepo = sp.GetRequiredService<DeviceRepository>();
        _receiptRepo = sp.GetRequiredService<ReceiptRepository>();
        _localeInfoRepo = sp.GetRequiredService<LocaleInfoRepository>();
        _withdrawalCassetteRepo = sp.GetRequiredService<WithdrawalCassetteRepository>();

        _withdrawalCassetteService = sp.GetRequiredService<WithdrawalCassetteService>();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await RunStepAsync("DB Connecting ...", InitializeDatabaseAsync);
            await RunStepAsync("Cache Loading ...", LoadStaticCacheAsync);
            await RunStepAsync("Language Loading ...", InitializeLocalizationAsync);
            await RunStepAsync("Devices Loading...", InitializeDevicesAsync);
            await RunStepAsync("Audio Preloading...", PreloadAudioAsync);

            IsInitialized = true;
            Update("Initialize Complete");
        }
        catch (Exception ex)
        {
            IsInitialized = false;

            // UI/VM에서 사용자에게 표시할 수 있도록 throw
            throw new Exception($"시스템 초기화 실패: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 초기화 스텝을 실행하며 공통 예외 처리 적용
    /// </summary>
    private async Task RunStepAsync(string message, Func<Task> step)
    {
        Update(message);

        try
        {
            await step();
            await Task.Delay(300); // UI 연출용, 필요 없으면 제거
        }
        catch (Exception ex)
        {
            _logging.Error(ex, $"[Init Step Failed] {message}");
            Update($"오류: {message}");
            throw; // 상위로 전달 → StartupViewModel에서 처리하도록
        }
    }

    private void Update(string msg)
    {
        ProgressChanged?.Invoke(msg);
        _logging.Info($"[Init] {msg}");
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (!await context.Database.CanConnectAsync().ConfigureAwait(false))
            throw new InvalidOperationException("DB 연결 실패");
    }

    private async Task LoadStaticCacheAsync()
    {
        var kiosks = await _kioskRepo.LoadAllAsync();
        _cache.Set(DatabaseCacheKeys.Kiosk, kiosks);
        if (kiosks.Count == 0)
        {
            _logging.Warn("[Init] Kiosk cache is empty. Skip loading dependent caches.");
            return;
        }
        
        _cache.Set(DatabaseCacheKeys.ApiConfigList, await _apiConfigRepo.LoadAllAsync());
        _cache.Set(DatabaseCacheKeys.DepositCurrencyList, await _depositCurrencyRepo.LoadByKioskIdAsync(kiosks[0].Id));
        _cache.Set(DatabaseCacheKeys.DeviceList, await _deviceRepo.LoadAllAsync());
        _cache.Set(DatabaseCacheKeys.ReceiptList, await _receiptRepo.LoadAllAsync());
        _cache.Set(DatabaseCacheKeys.LocaleInfoList, await _localeInfoRepo.LoadAllAsync());
        _cache.Set(DatabaseCacheKeys.WithdrawalCassetteList, await _withdrawalCassetteRepo.LoadAllAsync());

        await _withdrawalCassetteService.InitializeAsync();
    }

    private Task InitializeLocalizationAsync()
    {
        LocalizationProvider.Initialize(_localization);
        ResxLocalizationProvider.Initialize(_resxLocalization);
        var culture = CultureInfo.CurrentUICulture;
        _localization.SetCulture(culture);
        _resxLocalization.SetCulture(culture);
        return Task.CompletedTask;
    }

    private async Task InitializeDevicesAsync()
    {
        var devices = _cache.Get<IReadOnlyList<DeviceModel>>(DatabaseCacheKeys.DeviceList)
            ?? Array.Empty<DeviceModel>();
        foreach (var device in devices)
        {
            await _deviceManager.AddAsync(
                new DeviceDescriptor(
                    DeviceId: device.Id,
                    Name: device.Name,
                    Vendor: device.Vendor,
                    Model: device.Model,
                    TransportType: device.CommType,
                    TransportPort: device.CommPort,
                    TransportParam: device.CommParam,
                    ProtocolName: string.Empty,
                    PollingMs: device.PollingMs,
                    DeviceType: device.DeviceType,
                    Driver: device.DriverType
                ));
        }
    }

    private async Task PreloadAudioAsync()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        List<string> audioList =
        [
            Path.Combine(baseDir, "Assets", "Sound", "Click.wav"),
            Path.Combine(baseDir, "Assets", "Sound", "Bill.wav"),
            Path.Combine(baseDir, "Assets", "Sound", "Coin.wav"),
        ];

        await _audioService.PreloadAllAsync(audioList);
    }
}
