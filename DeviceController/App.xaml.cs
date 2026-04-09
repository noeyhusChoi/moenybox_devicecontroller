using System.IO;
using System.Windows;
using System.Windows.Threading;
using DeviceController.Services;
using DeviceController.ViewModels;
using DeviceKit.Engine;

namespace DeviceController;

public partial class App : Application
{
    private IDeviceManagerPort? _deviceManager;
    private MainWindowViewModel? _mainWindowViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        base.OnStartup(e);

        var environmentDiagnostics = RuntimeEnvironment.ConfigureProcessPaths();
        var descriptorPath = Path.Combine(AppContext.BaseDirectory, "device-descriptors.json");
        var descriptorLoad = DeviceDescriptorLoader
            .LoadAsync(descriptorPath)
            .GetAwaiter()
            .GetResult();

        var runtimeOptionsPath = Path.Combine(AppContext.BaseDirectory, "runtime-options.json");
        var runtimeOptions = RuntimeOptionsLoader.LoadOrDefault(runtimeOptionsPath);

        _deviceManager = new DeferredDeviceManagerPort(descriptorLoad.Descriptors, runtimeOptions);
        _mainWindowViewModel = new MainWindowViewModel(
            _deviceManager,
            descriptorLoad.Summary,
            $"{environmentDiagnostics}{Environment.NewLine}{descriptorLoad.Diagnostics}");

        var window = new MainWindow
        {
            Title = $"Device Controller [{descriptorLoad.SourceLabel}]",
            DataContext = _mainWindowViewModel
        };

        Current.MainWindow = window;
        window.Show();
        window.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => _ = _mainWindowViewModel.InitializeAsync()));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainWindowViewModel?.Dispose();
        _mainWindowViewModel = null;

        if (_deviceManager is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _deviceManager = null;
        }

        base.OnExit(e);
    }
}
