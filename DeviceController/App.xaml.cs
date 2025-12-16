using System.Threading;
using System.Windows;
using DeviceController.Core.Abstractions;
using DeviceController.Devices.Diagnostics;
using DeviceController.Devices.Simulated;
using DeviceController.Devices.Scanner;
using DeviceController.Services;
using DeviceController.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeviceController
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private StatusPollingService? _statusPolling;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var registry = _serviceProvider.GetRequiredService<IDeviceRegistry>();
            await registry.StartAsync(CancellationToken.None);

            _statusPolling = _serviceProvider.GetRequiredService<StatusPollingService>();
            await _statusPolling.StartAsync(CancellationToken.None);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider?.GetService<IDeviceRegistry>() is { } registry)
            {
                await registry.StopAsync(CancellationToken.None);
            }

            if (_statusPolling != null)
            {
                await _statusPolling.StopAsync();
                await _statusPolling.DisposeAsync();
            }

            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SimulatedProtocol>();
            services.AddSingleton<DiagnosticsProtocol>();
            services.AddSingleton<ScannerProtocol>();
            services.AddSingleton<IDecodeEventBus, DecodeEventBus>();

            var simulatedConfigs = new[]
            {
                new SimulatedDeviceConfig("Simulated-01", "SimulatedSerial-01"),
                new SimulatedDeviceConfig("Simulated-02", "SimulatedSerial-02")
            };

            foreach (var config in simulatedConfigs)
            {
                services.AddSingleton<IDevice>(sp =>
                    new SimulatedDevice(config.DeviceId, new SimulatedDeviceClient(config.ClientId),
                        sp.GetRequiredService<SimulatedProtocol>()));
            }

            var diagnosticsConfigs = new[]
            {
                new DiagnosticsDeviceConfig("Diagnostics-01", "Tcp-01")
            };

            foreach (var config in diagnosticsConfigs)
            {
                services.AddSingleton<IDevice>(sp =>
                    new DiagnosticsDevice(config.DeviceId, new DiagnosticsDeviceClient(config.ClientId),
                        sp.GetRequiredService<DiagnosticsProtocol>()));
            }

            var scannerConfigs = new[]
            {
                new ScannerDeviceConfig("Scanner-01", "COM3", 115200)
            };

            foreach (var config in scannerConfigs)
            {
                services.AddSingleton<IDevice>(sp =>
                    new ScannerDevice(config.DeviceId, new ScannerClient(config), sp.GetRequiredService<ScannerProtocol>()));
            }

            services.AddSingleton<IDeviceRegistry, DeviceRegistry>();
            services.AddSingleton(sp => new StatusPollingService(sp.GetRequiredService<IDeviceRegistry>(), TimeSpan.FromSeconds(2)));
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }
    }
}
