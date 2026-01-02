using KIOSK.Device.Abstractions;
using KIOSK.Devices.Management;
using KIOSK.Infrastructure.Cache;
using KIOSK.Infrastructure.Database;
using KIOSK.Infrastructure.Database.Repositories;
using KIOSK.Infrastructure.Initialization;
using KIOSK.Infrastructure.Logging;
using KIOSK.Infrastructure.Media;
using KIOSK.Services;
using Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.IO;

public class AppInitializer : IAppInitializer
{
    private readonly IDatabaseService _db;
    private readonly ILocalizationService _localization;
    private readonly ILoggingService _logging;
    private readonly IAudioPlayService _audioService;
    private readonly IDeviceManager _deviceManager;

    private readonly DatabaseCache _staticCache;
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
        _db = sp.GetRequiredService<IDatabaseService>();
        _localization = sp.GetRequiredService<ILocalizationService>();
        _logging = sp.GetRequiredService<ILoggingService>();
        _audioService = sp.GetRequiredService<IAudioPlayService>();
        _deviceManager = sp.GetRequiredService<IDeviceManager>();

        _staticCache = sp.GetRequiredService<DatabaseCache>();
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
            throw; // 상위로 전달 → StartupWindowVM에서 처리하도록
        }
    }

    private void Update(string msg)
    {
        ProgressChanged?.Invoke(msg);
        _logging.Info($"[Init] {msg}");
    }

    private async Task InitializeDatabaseAsync()
    {
        if (!await _db.CanConnectAsync())
            throw new InvalidOperationException("DB 연결 실패");
    }

    private async Task LoadStaticCacheAsync()
    {
        _staticCache.ApiConfigList = await _apiConfigRepo.LoadAllAsync();
        _staticCache.DepositCurrencyList = await _depositCurrencyRepo.LoadAllAsync();
        _staticCache.Kiosk = await _kioskRepo.LoadAllAsync();
        _staticCache.DeviceList = await _deviceRepo.LoadAllAsync();
        _staticCache.ReceiptList = await _receiptRepo.LoadAllAsync();
        _staticCache.LocaleInfoList = await _localeInfoRepo.LoadAllAsync();
        _staticCache.WithdrawalCassetteList = await _withdrawalCassetteRepo.LoadAllAsync();


        await _withdrawalCassetteService.InitializeAsync();
    }

    private Task InitializeLocalizationAsync()
    {
        LocalizationProvider.Initialize(_localization);
        var culture = CultureInfo.CurrentUICulture;
        _localization.SetCulture(culture);
        return Task.CompletedTask;
    }

    private async Task InitializeDevicesAsync()
    {
        foreach (var device in _staticCache.DeviceList)
        {
            await _deviceManager.AddAsync(
                new DeviceDescriptor(
                    Name: device.Id,
                    Model: device.Type,
                    TransportType: device.CommType,
                    TransportPort: device.CommPort,
                    TransportParam: device.CommParam,
                    ProtocolName: string.Empty
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
