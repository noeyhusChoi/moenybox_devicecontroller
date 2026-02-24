using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using KIOSK.Admin.Services;
using KIOSK.DeviceRuntime.Ports;
using KIOSK.Admin.ViewModels;
using KIOSK.Infrastructure.Database;

namespace KIOSK.Admin;

public partial class App : System.Windows.Application
{
    private DeviceRuntimePort? _runtimePort;
    private DeviceRuntimeStatusMessengerBridge? _statusBridge;

    protected override void OnStartup(StartupEventArgs e)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        base.OnStartup(e);

        var descriptorPath = Path.Combine(AppContext.BaseDirectory, "device-descriptors.json");
        var connectionString =
            Environment.GetEnvironmentVariable("KIOSK_ADMIN_DB_CONNECTION_STRING") ??
            Environment.GetEnvironmentVariable("KIOSK_DB_CONNECTION_STRING") ??
            DatabaseConfig.DefaultConnectionString;
        var descriptors = AdminDeviceDescriptors
            .LoadFromDatabaseJsonOrDefaultAsync(connectionString, descriptorPath)
            .GetAwaiter()
            .GetResult();
        var runtimeOptionsPath = Path.Combine(AppContext.BaseDirectory, "runtime-options.json");
        var runtimeOptions = AdminRuntimeOptionsLoader.LoadOrDefault(runtimeOptionsPath);
        var messenger = new WeakReferenceMessenger();
        var commandCatalog = new AdminDeviceCommandCatalog(descriptors);

        _runtimePort = new DeviceRuntimePort(descriptors, runtimeOptions);
        _statusBridge = new DeviceRuntimeStatusMessengerBridge(_runtimePort, messenger);
        var vm = new MainWindowViewModel(_runtimePort, commandCatalog, messenger);
        var window = new MainWindow
        {
            Title = "KIOSK Admin - Device Monitor",
            DataContext = vm
        };
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _statusBridge?.Dispose();
        _statusBridge = null;

        if (_runtimePort is not null)
        {
            _runtimePort.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _runtimePort = null;
        }

        base.OnExit(e);
    }
}
 
