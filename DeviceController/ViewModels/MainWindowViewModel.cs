using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeviceController.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IDeviceManagerPort _deviceManager;

    public MainWindowViewModel(
        IDeviceManagerPort deviceManager,
        string summaryText,
        string diagnosticsText)
    {
        _deviceManager = deviceManager;
        SummaryText = summaryText;
        DiagnosticsText = diagnosticsText;
        LastActionText = "Ready";

        SeedDevices();
        _deviceManager.DeviceStatusObserved += OnStatusObserved;
        _deviceManager.ConnectionObserved += OnConnectionObserved;
        _deviceManager.DeviceEventReceived += OnDeviceEventReceived;

        AppendLog(summaryText);
        foreach (var line in diagnosticsText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            AppendLog(line);
    }

    public Task InitializeAsync() => RefreshAllAsync();

    public ObservableCollection<DeviceRowViewModel> Devices { get; } = new();
    public ObservableCollection<string> AvailableCommands { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();

    [ObservableProperty]
    private string summaryText = string.Empty;

    [ObservableProperty]
    private string diagnosticsText = string.Empty;

    [ObservableProperty]
    private string lastActionText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshSelectedStatusCommand))]
    [NotifyCanExecuteChangedFor(nameof(DepositStartCommand))]
    [NotifyCanExecuteChangedFor(nameof(DepositStopCommand))]
    [NotifyCanExecuteChangedFor(nameof(DepositStackCommand))]
    [NotifyCanExecuteChangedFor(nameof(DepositReturnCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScannerStartCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScannerStopCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScannerSaveImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScannerGetScanStatusCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScannerRunOcrCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScannerGetDeviceIdCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrinterPrintTitleCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrinterPrintContentCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrinterCutCommand))]
    [NotifyCanExecuteChangedFor(nameof(QrEnableCommand))]
    [NotifyCanExecuteChangedFor(nameof(QrDisableCommand))]
    [NotifyCanExecuteChangedFor(nameof(WithdrawalSensorCommand))]
    [NotifyCanExecuteChangedFor(nameof(WithdrawalInitCommand))]
    [NotifyCanExecuteChangedFor(nameof(WithdrawalEjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(WithdrawalDispenseCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteGenericCommand))]
    private DeviceRowViewModel? selectedDevice;

    partial void OnSelectedDeviceChanged(DeviceRowViewModel? value)
    {
        RefreshAvailableCommands(value?.DeviceId);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteGenericCommand))]
    private string selectedGenericCommandName = string.Empty;

    [ObservableProperty]
    private string genericPayloadText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrinterPrintTitleCommand))]
    private string printerTitleText = "Test Title";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrinterPrintContentCommand))]
    private string printerContentText = "Test Content";

    [ObservableProperty]
    private string runOcrPayloadText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(WithdrawalEjectCommand))]
    private string withdrawalEjectValueText = "0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(WithdrawalDispenseCommand))]
    private string withdrawalDispenseText = "1:1";

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
        AppendLog("Logs cleared.");
    }

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        var statuses = await _deviceManager.GetStatusesAsync().ConfigureAwait(false);
        var connections = await _deviceManager.GetConnectionsAsync().ConfigureAwait(false);

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            var statusMap = statuses.ToDictionary(x => x.DeviceId, StringComparer.OrdinalIgnoreCase);
            var connectionMap = connections.ToDictionary(x => x.DeviceId, StringComparer.OrdinalIgnoreCase);

            foreach (var row in Devices)
            {
                if (statusMap.TryGetValue(row.DeviceId, out var status))
                    row.UpdateStatus(status);

                if (connectionMap.TryGetValue(row.DeviceId, out var connection))
                    row.UpdateConnection(connection);
            }

            SummaryText = MergeDeviceCount(Devices.Count);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedDevice))]
    private async Task ConnectAsync()
    {
        await ExecuteControlAsync(
            async ct => await _deviceManager.ConnectAsync(SelectedDevice!.DeviceId, ct).ConfigureAwait(false),
            changed => changed ? "Connect requested." : "Connect skipped.");
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedDevice))]
    private async Task DisconnectAsync()
    {
        await ExecuteControlAsync(
            async ct => await _deviceManager.DisconnectAsync(SelectedDevice!.DeviceId, ct).ConfigureAwait(false),
            changed => changed ? "Disconnect requested." : "Disconnect skipped.");
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedDevice))]
    private async Task RefreshSelectedStatusAsync()
    {
        if (SelectedDevice is null)
            return;

        var status = await _deviceManager.GetStatusAsync(SelectedDevice.DeviceId).ConfigureAwait(false);
        if (status is null)
        {
            LastActionText = $"[{SelectedDevice.DeviceId}] status unavailable";
            AppendLog(LastActionText);
            return;
        }

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            SelectedDevice.UpdateStatus(status);
            LastActionText = $"[{SelectedDevice.DeviceId}] status refreshed";
            AppendLog(LastActionText);
        });
    }

    [RelayCommand(CanExecute = nameof(CanRunDeposit))]
    private Task DepositStartAsync() => ExecuteDeviceCommandAsync(DepositCommands.Start(), "Deposit START");

    [RelayCommand(CanExecute = nameof(CanRunDeposit))]
    private Task DepositStopAsync() => ExecuteDeviceCommandAsync(DepositCommands.Stop(), "Deposit STOP");

    [RelayCommand(CanExecute = nameof(CanRunDeposit))]
    private Task DepositStackAsync() => ExecuteDeviceCommandAsync(DepositCommands.Stack(), "Deposit STACK");

    [RelayCommand(CanExecute = nameof(CanRunDeposit))]
    private Task DepositReturnAsync() => ExecuteDeviceCommandAsync(DepositCommands.Return(), "Deposit RETURN");

    [RelayCommand(CanExecute = nameof(CanRunScanner))]
    private Task ScannerStartAsync() => ExecuteDeviceCommandAsync(IdScannerCommands.ScanStart(), "Scanner SCANSTART");

    [RelayCommand(CanExecute = nameof(CanRunScanner))]
    private Task ScannerStopAsync() => ExecuteDeviceCommandAsync(IdScannerCommands.ScanStop(), "Scanner SCANSTOP");

    [RelayCommand(CanExecute = nameof(CanRunScanner))]
    private Task ScannerSaveImageAsync() => ExecuteDeviceCommandAsync(IdScannerCommands.SaveImage(), "Scanner SAVEIMAGE");

    [RelayCommand(CanExecute = nameof(CanRunScanner))]
    private Task ScannerGetScanStatusAsync() => ExecuteDeviceCommandAsync(IdScannerCommands.GetScanStatus(), "Scanner GETSCANSTATUS");

    [RelayCommand(CanExecute = nameof(CanRunScanner))]
    private Task ScannerRunOcrAsync()
    {
        var payload = string.IsNullOrWhiteSpace(RunOcrPayloadText) ? null : RunOcrPayloadText;
        return ExecuteDeviceCommandAsync(new DeviceCommandRequest("RUNOCR", payload), "Scanner RUNOCR");
    }

    [RelayCommand(CanExecute = nameof(CanRunScanner))]
    private Task ScannerGetDeviceIdAsync() => ExecuteDeviceCommandAsync(new DeviceCommandRequest("GETDEVICEID"), "Scanner GETDEVICEID");

    [RelayCommand(CanExecute = nameof(CanRunPrinterTitle))]
    private Task PrinterPrintTitleAsync() => ExecuteDeviceCommandAsync(PrinterCommands.PrintTitle(PrinterTitleText), "Printer PRINTTITLE");

    [RelayCommand(CanExecute = nameof(CanRunPrinterContent))]
    private Task PrinterPrintContentAsync() => ExecuteDeviceCommandAsync(PrinterCommands.PrintContent(PrinterContentText), "Printer PRINTCONTENT");

    [RelayCommand(CanExecute = nameof(CanRunPrinter))]
    private Task PrinterCutAsync() => ExecuteDeviceCommandAsync(PrinterCommands.Cut(), "Printer CUT");

    [RelayCommand(CanExecute = nameof(CanRunQr))]
    private Task QrEnableAsync() => ExecuteDeviceCommandAsync(QrCommands.Enable(), "QR SCAN_ENABLE");

    [RelayCommand(CanExecute = nameof(CanRunQr))]
    private Task QrDisableAsync() => ExecuteDeviceCommandAsync(QrCommands.Disable(), "QR SCAN_DISABLE");

    [RelayCommand(CanExecute = nameof(CanRunWithdrawal))]
    private Task WithdrawalSensorAsync() => ExecuteDeviceCommandAsync(WithdrawalCommands.Sensor(), "Withdrawal SENSOR");

    [RelayCommand(CanExecute = nameof(CanRunWithdrawal))]
    private Task WithdrawalInitAsync() => ExecuteDeviceCommandAsync(WithdrawalCommands.Init(), "Withdrawal INIT");

    [RelayCommand(CanExecute = nameof(CanRunWithdrawalEject))]
    private Task WithdrawalEjectAsync()
        => ExecuteDeviceCommandAsync(
            WithdrawalCommands.Eject(new WithdrawalEjectRequest(WithdrawalEjectValueText)),
            "Withdrawal EJECT");

    [RelayCommand(CanExecute = nameof(CanRunWithdrawalDispense))]
    private Task WithdrawalDispenseAsync()
        => ExecuteDeviceCommandAsync(
            WithdrawalCommands.Dispense(ParseDispenseRequests(WithdrawalDispenseText)),
            "Withdrawal DISPENSE");

    [RelayCommand(CanExecute = nameof(CanRunGeneric))]
    private Task ExecuteGenericAsync()
    {
        var payload = string.IsNullOrWhiteSpace(GenericPayloadText) ? null : GenericPayloadText;
        return ExecuteDeviceCommandAsync(
            new DeviceCommandRequest(SelectedGenericCommandName, payload),
            $"Generic {SelectedGenericCommandName}");
    }

    private bool CanUseSelectedDevice() => SelectedDevice is not null;
    private bool CanRunDeposit() => HasSelectedType("DEPOSIT");
    private bool CanRunScanner() => HasSelectedType("IDSCANNER");
    private bool CanRunPrinter() => HasSelectedType("PRINTER");
    private bool CanRunPrinterTitle() => CanRunPrinter() && !string.IsNullOrWhiteSpace(PrinterTitleText);
    private bool CanRunPrinterContent() => CanRunPrinter() && !string.IsNullOrWhiteSpace(PrinterContentText);
    private bool CanRunQr() => HasSelectedType("QR");
    private bool CanRunWithdrawal() => HasSelectedType("WITHDRAWAL");
    private bool CanRunWithdrawalEject() => CanRunWithdrawal() && !string.IsNullOrWhiteSpace(WithdrawalEjectValueText);
    private bool CanRunWithdrawalDispense() => CanRunWithdrawal() && !string.IsNullOrWhiteSpace(WithdrawalDispenseText);
    private bool CanRunGeneric() => SelectedDevice is not null && !string.IsNullOrWhiteSpace(SelectedGenericCommandName);

    public void Dispose()
    {
        _deviceManager.DeviceStatusObserved -= OnStatusObserved;
        _deviceManager.ConnectionObserved -= OnConnectionObserved;
        _deviceManager.DeviceEventReceived -= OnDeviceEventReceived;
    }

    private void SeedDevices()
    {
        foreach (var descriptor in _deviceManager.GetAllDevices())
            Devices.Add(new DeviceRowViewModel(descriptor));

        SummaryText = MergeDeviceCount(Devices.Count);
    }

    private async Task ExecuteControlAsync(
        Func<CancellationToken, Task<bool>> action,
        Func<bool, string> messageFactory,
        CancellationToken cancellationToken = default)
    {
        if (SelectedDevice is null)
            return;

        try
        {
            var changed = await action(cancellationToken).ConfigureAwait(false);
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                LastActionText = $"[{SelectedDevice.DeviceId}] {messageFactory(changed)}";
                AppendLog(LastActionText);
            });
        }
        catch (Exception ex)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                LastActionText = $"[{SelectedDevice.DeviceId}] failed - {ex.Message}";
                AppendLog(LastActionText);
            });
        }
    }

    private async Task ExecuteDeviceCommandAsync(
        DeviceCommandRequest request,
        string actionLabel,
        CancellationToken cancellationToken = default)
    {
        if (SelectedDevice is null)
            return;

        try
        {
            var result = await _deviceManager
                .ExecuteAsync(SelectedDevice.DeviceId, request, cancellationToken)
                .ConfigureAwait(false);

            var dataText = FormatData(result.Data);
            var codeText = result.Code?.ToString() ?? (result.Success ? "OK" : "FAILED");
            var builder = new StringBuilder()
                .Append('[').Append(SelectedDevice.DeviceId).Append("] ")
                .Append(actionLabel)
                .Append(" => ")
                .Append(codeText)
                .Append(" | success=").Append(result.Success);

            if (!string.IsNullOrWhiteSpace(result.Message))
                builder.Append(" | message=").Append(result.Message);
            if (!string.IsNullOrWhiteSpace(dataText))
                builder.Append(" | data=").Append(dataText);

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                LastActionText = builder.ToString();
                AppendLog(LastActionText);
            });
        }
        catch (Exception ex)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                LastActionText = $"[{SelectedDevice.DeviceId}] {actionLabel} failed - {ex.Message}";
                AppendLog(LastActionText);
            });
        }
    }

    private void OnStatusObserved(StatusSnapshot snapshot)
    {
        _ = App.Current.Dispatcher.InvokeAsync(() =>
        {
            if (TryGetRow(snapshot.DeviceId, out var row))
            {
                row.UpdateStatus(snapshot);
                if (SelectedDevice == row)
                    RefreshAvailableCommands(row.DeviceId);
            }
        });
    }

    private void OnConnectionObserved(DeviceConnectionSnapshot snapshot)
    {
        _ = App.Current.Dispatcher.InvokeAsync(() =>
        {
            if (TryGetRow(snapshot.DeviceId, out var row))
            {
                row.UpdateConnection(snapshot);
                if (SelectedDevice == row)
                    RefreshAvailableCommands(row.DeviceId);
            }

            AppendLog($"[{snapshot.DeviceId}] connection => {snapshot.State}");
        });
    }

    private void OnDeviceEventReceived(DeviceEventEnvelope envelope)
    {
        _ = App.Current.Dispatcher.InvokeAsync(() =>
        {
            AppendLog($"[{envelope.DeviceId}] event={envelope.EventName} payload={envelope.PayloadJson}");
        });
    }

    private void RefreshAvailableCommands(string? deviceId)
    {
        AvailableCommands.Clear();
        SelectedGenericCommandName = string.Empty;

        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        foreach (var command in _deviceManager.GetCommands(deviceId))
            AvailableCommands.Add(command);

        if (AvailableCommands.Count > 0)
            SelectedGenericCommandName = AvailableCommands[0];
    }

    private bool TryGetRow(string deviceId, out DeviceRowViewModel row)
    {
        row = Devices.FirstOrDefault(x => string.Equals(x.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))!;
        return row is not null;
    }

    private bool HasSelectedType(string deviceType)
        => SelectedDevice is not null &&
           string.Equals(SelectedDevice.DeviceType, deviceType, StringComparison.OrdinalIgnoreCase);

    private void AppendLog(string message)
    {
        LogEntries.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
        while (LogEntries.Count > 400)
            LogEntries.RemoveAt(LogEntries.Count - 1);
    }

    private string MergeDeviceCount(int count)
    {
        var prefix = SummaryText;
        var separatorIndex = SummaryText.IndexOf(" | Devices:", StringComparison.Ordinal);
        if (separatorIndex >= 0)
            prefix = SummaryText[..separatorIndex];

        return $"{prefix} | Devices: {count}";
    }

    private static string FormatData(object? data)
    {
        if (data is null)
            return string.Empty;

        if (data is byte[] bytes)
            return $"byte[{bytes.Length}]";

        try
        {
            return JsonSerializer.Serialize(data);
        }
        catch
        {
            return data.ToString() ?? string.Empty;
        }
    }

    private static IReadOnlyList<WithdrawalDispenseSlotRequest> ParseDispenseRequests(string input)
    {
        var results = new List<WithdrawalDispenseSlotRequest>();
        foreach (var token in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = token.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out var slot) ||
                !int.TryParse(parts[1], out var count) ||
                count <= 0)
            {
                throw new InvalidOperationException("Dispense format must be 'slot:count,slot:count'. Example: 1:2,2:1");
            }

            results.Add(new WithdrawalDispenseSlotRequest(slot, count));
        }

        if (results.Count == 0)
            throw new InvalidOperationException("At least one dispense request is required.");

        return results;
    }
}

public partial class DeviceRowViewModel : ObservableObject
{
    public DeviceRowViewModel(DeviceDescriptor descriptor)
    {
        DeviceId = descriptor.EffectiveId;
        Name = descriptor.Name;
        Vendor = descriptor.Vendor;
        Model = descriptor.Model;
        DriverType = descriptor.DriverType;
        TransportType = descriptor.TransportType;
        TransportPort = descriptor.TransportPort;
        TransportParam = descriptor.TransportParam;
        DeviceType = descriptor.DeviceType;
        Message = "Waiting";
        Timestamp = DateTimeOffset.UtcNow;
    }

    public string DeviceId { get; }
    public string Name { get; }
    public string Vendor { get; }
    public string Model { get; }
    public string DriverType { get; }
    public string TransportType { get; }
    public string TransportPort { get; }
    public string TransportParam { get; }

    [ObservableProperty]
    private string deviceType = string.Empty;

    [ObservableProperty]
    private DeviceConnectionState connectionState;

    [ObservableProperty]
    private bool isHealthy;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private DateTimeOffset timestamp;

    public void UpdateStatus(StatusSnapshot snapshot)
    {
        DeviceType = snapshot.DeviceType;
        IsHealthy = snapshot.IsHealthy;
        Timestamp = snapshot.Timestamp;
        Message = snapshot.Alerts.Count == 0
            ? "Healthy"
            : string.Join(" | ", snapshot.Alerts.Select(x => x.Message));
    }

    public void UpdateConnection(DeviceConnectionSnapshot snapshot)
    {
        DeviceType = snapshot.DeviceType;
        ConnectionState = snapshot.State;
        Timestamp = snapshot.Timestamp;
        Message = snapshot.State.ToString();
    }
}
