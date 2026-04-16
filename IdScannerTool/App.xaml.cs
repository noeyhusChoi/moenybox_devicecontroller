using DeviceKit.Engine;
using IdScannerTool.Services;
using IdScannerTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using Velopack;

namespace IdScannerTool;

public partial class App : Application
{
    private IHost? _host;

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        base.OnStartup(e);

        _host = BuildHost();
        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            try
            {
                _host.StopAsync(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                if (_host is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync()
                        .AsTask()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    _host.Dispose();
                }

                _host = null;
            }
        }

        base.OnExit(e);
    }

    private static IHost BuildHost()
    {
        var installRoot = AppContext.BaseDirectory;
        var appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Moneybox",
            "IdScannerTool");
        var configPath = Path.Combine(installRoot, "Config.ini");
        var serialKeyPath = Path.Combine(appDataRoot, "serial-key.json");
        var apiKeyPath = Path.Combine(appDataRoot, "api-key.json");
        var ocrDbPath = Path.Combine(appDataRoot, "ocr-history.db");
        var externalOcrAssetRoot = Path.Combine(installRoot, "OCR");
        var externalOcrDataRoot = Path.Combine(appDataRoot, "OCR");
        var externalOcrExecutablePath = Path.Combine(externalOcrAssetRoot, "moneybox_ocr.exe");
        var descriptor = BuildIdScannerDescriptor();
        var runtimeOptions = BuildRuntimeOptions();
        var externalOcrOptions = new ExternalOcrOptions(
            InputDir: Path.Combine(externalOcrDataRoot, "Input"),
            ResultDir: Path.Combine(externalOcrDataRoot, "Result"),
            ResultTypeDir: Path.Combine(externalOcrDataRoot, "ResultType"),
            ResultTimeout: TimeSpan.FromSeconds(10),
            PollInterval: TimeSpan.FromMilliseconds(200));
        var externalOcrProcessOptions = new ExternalOcrProcessOptions(
            ExecutablePath: externalOcrExecutablePath);

        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(descriptor);
                services.AddSingleton(runtimeOptions);
                services.AddSingleton(externalOcrOptions);
                services.AddSingleton(externalOcrProcessOptions);
                services.AddSingleton(_ => new HttpClient
                {
                    BaseAddress = new Uri("https://dxjhw783se.execute-api.ap-northeast-2.amazonaws.com/prod/"),
                    Timeout = TimeSpan.FromSeconds(15)
                });

                services.AddSingleton<ILocalSerialKeyStore>(_ => new LocalSerialKeyStore(serialKeyPath));
                services.AddSingleton<IApiKeyStore>(_ => new LocalApiKeyStore(apiKeyPath));
                services.AddSingleton<ISerialRegistrationStateService, SerialRegistrationStateService>();
                services.AddSingleton<IStartupSequenceService, StartupSequenceService>();
                services.AddSingleton<IDeviceApiClient, DeviceApiClient>();
                services.AddSingleton<IAppUpdateService>(_ => new AppUpdateService(configPath));
                services.AddSingleton<IOcrHistoryStore>(_ => new OcrSqliteStore(ocrDbPath));
                services.AddSingleton<IHistoryExcelExportService, HistoryExcelExportService>();
                services.AddSingleton<IAppOverlayService, AppOverlayService>();

                services.AddSingleton<IDeviceManagerPort>(sp =>
                    new DeviceRuntimePort(
                        new[] { sp.GetRequiredService<DeviceDescriptor>() },
                        sp.GetRequiredService<DeviceRuntimeOptions>()));

                services.AddSingleton<IScanSessionService>(sp =>
                    new ScanSessionService(
                        sp.GetRequiredService<IDeviceManagerPort>(),
                        sp.GetRequiredService<DeviceDescriptor>().EffectiveId));
                services.AddSingleton<DeviceConnectionMonitorService>(sp =>
                    new DeviceConnectionMonitorService(
                        sp.GetRequiredService<IDeviceManagerPort>(),
                        sp.GetRequiredService<DeviceDescriptor>().EffectiveId));
                services.AddSingleton<IDeviceConnectionMonitorService>(sp => sp.GetRequiredService<DeviceConnectionMonitorService>());
                services.AddHostedService(sp => sp.GetRequiredService<DeviceConnectionMonitorService>());

                services.AddSingleton<IInternalOcrService, InternalOcrService>();
                services.AddSingleton<IExternalOcrService, ExternalOcrService>();
                services.AddSingleton<IOcrProcessingService, OcrPipelineService>();
                services.AddSingleton<IOcrResultConverter, OcrResultConverter>();
                services.AddSingleton<MoneyboxOcrHostService>();
                services.AddHostedService(sp => sp.GetRequiredService<MoneyboxOcrHostService>());

                services.AddSingleton<IStartupVerificationService>(sp =>
                    new StartupVerificationService(
                        sp.GetRequiredService<IDeviceManagerPort>(),
                        sp.GetRequiredService<DeviceDescriptor>().EffectiveId));

                services.AddSingleton<MainRuntimeViewModel>(sp =>
                    new MainRuntimeViewModel(
                        sp.GetRequiredService<IDeviceManagerPort>(),
                        sp.GetRequiredService<IOcrHistoryStore>(),
                        sp.GetRequiredService<IHistoryExcelExportService>(),
                        sp.GetRequiredService<IAppOverlayService>(),
                        sp.GetRequiredService<IOcrProcessingService>(),
                        sp.GetRequiredService<IOcrResultConverter>(),
                        sp.GetRequiredService<IScanSessionService>(),
                        sp.GetRequiredService<IDeviceApiClient>(),
                        sp.GetRequiredService<IApiKeyStore>(),
                        sp.GetRequiredService<DeviceDescriptor>().EffectiveId));
                services.AddSingleton<ShellViewModel>(sp =>
                    ShellViewModel.Create(
                        sp.GetRequiredService<MainRuntimeViewModel>(),
                        sp.GetRequiredService<IAppOverlayService>(),
                        sp.GetRequiredService<IStartupSequenceService>(),
                        sp.GetRequiredService<IDeviceConnectionMonitorService>(),
                        sp.GetRequiredService<IAppUpdateService>()));

                services.AddSingleton<MainWindow>(sp => new MainWindow
                {
                    Title = "M-Box ID Scanner",
                    DataContext = sp.GetRequiredService<ShellViewModel>()
                });
            })
            .Build();
    }

    private static DeviceDescriptor BuildIdScannerDescriptor()
        => new(
            Name: "IDSCANNER1",
            Vendor: "DEFAULT",
            Model: "COMBOSCAN2208",
            TransportType: "IDSCANNER",
            TransportPort: string.Empty,
            TransportParam: string.Empty,
            ProtocolName: string.Empty,
            PollingMs: 10000,
            Validate: true,
            DeviceType: "IDSCANNER",
            DriverType: "COMBOSCAN2208",
            DeviceId: "IDSCANNER1");

    private static DeviceRuntimeOptions BuildRuntimeOptions()
        => DeviceRuntimeOptions.Default with
        {
            DefaultPollingMs = 1000,
            MinPollingMs = 1000,
            MaxBackoffMs = 60000,
            SchedulerTickMs = 500
        };
}
