using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IdScannerTool.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;

namespace IdScannerTool.ViewModels;

public partial class MainRuntimeViewModel : ObservableObject
{
    private static readonly TimeSpan AutoStandbyTick = TimeSpan.FromMilliseconds(1000);

    private readonly IDeviceManagerPort _runtimePort;
    private readonly IOcrHistoryStore _ocrHistoryStore;
    private readonly IHistoryExcelExportService _historyExcelExportService;
    private readonly IAppOverlayService _appOverlayService;
    private readonly IOcrProcessingService _ocrProcessingService;
    private readonly IOcrResultConverter _ocrResultConverter;
    private readonly IScanSessionService _scanSessionService;
    private readonly IDeviceApiClient _deviceApiClient;
    private readonly IApiKeyStore _apiKeyStore;
    private readonly SemaphoreSlim _usageSyncLock = new(1, 1);
    private readonly object _operationSync = new();
    private readonly object _autoSync = new();
    private CancellationTokenSource? _activeOperationCts;
    private CancellationTokenSource? _autoStandbyCts;
    private Task? _autoStandbyTask;
    private bool _autoStandbyEnabled;
    private bool _pauseAutoStandby;
    private bool _awaitingEmptyAfterScan;
    private bool _detectedHandling;
    private bool _suspendSelectAllSync;
    private SaveImageResultDto? _lastCaptured;
    private IReadOnlyList<OcrHistoryRow> _allHistoryRows = Array.Empty<OcrHistoryRow>();

    public MainRuntimeViewModel(
        IDeviceManagerPort runtimePort,
        IOcrHistoryStore ocrHistoryStore,
        IHistoryExcelExportService historyExcelExportService,
        IAppOverlayService appOverlayService,
        IOcrProcessingService ocrProcessingService,
        IOcrResultConverter ocrResultConverter,
        IScanSessionService scanSessionService,
        IDeviceApiClient deviceApiClient,
        IApiKeyStore apiKeyStore,
        string deviceId)
    {
        _runtimePort = runtimePort;
        _ocrHistoryStore = ocrHistoryStore;
        _historyExcelExportService = historyExcelExportService;
        _appOverlayService = appOverlayService;
        _ocrProcessingService = ocrProcessingService;
        _ocrResultConverter = ocrResultConverter;
        _scanSessionService = scanSessionService;
        _deviceApiClient = deviceApiClient;
        _apiKeyStore = apiKeyStore;
        _scanSessionService.ProgressChanged += OnScanSessionProgressChanged;
        DeviceId = deviceId;
        EnsureDefaultHistoryDateRange();
        _ = InitializeAsync();
    }

    public string DeviceId { get; }

    [ObservableProperty]
    private string deviceType = "IDSCANNER";

    [ObservableProperty]
    private DeviceConnectionState connectionState = DeviceConnectionState.Disconnected;

    [ObservableProperty]
    private bool isHealthy;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private DateTimeOffset timestamp = DateTimeOffset.MinValue;

    [ObservableProperty]
    private string lastResult = "Ready";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string scanPresence = "-";

    [ObservableProperty]
    private bool isDetected;

    [ObservableProperty]
    private bool isScanPolling;

    [ObservableProperty]
    private string ocrStatus = "-";

    [ObservableProperty]
    private OcrHistoryItem? selectedOcrHistory;

    [ObservableProperty]
    private bool isAllHistorySelected;

    [ObservableProperty]
    private string searchNo = string.Empty;

    [ObservableProperty]
    private string searchName = string.Empty;

    [ObservableProperty]
    private DateTime? searchStartDate;

    [ObservableProperty]
    private DateTime? searchEndDate;

    partial void OnIsAllHistorySelectedChanged(bool value)
    {
        if (_suspendSelectAllSync)
        {
            return;
        }

        foreach (var row in OcrHistory)
        {
            row.IsSelected = value;
        }
    }

    public ObservableCollection<OcrFieldItem> OcrFields { get; } = new();
    public ObservableCollection<OcrHistoryItem> OcrHistory { get; } = new();

    public void EnsureDefaultHistoryDateRange()
    {
        var today = DateTime.Today;
        SearchStartDate ??= today;
        SearchEndDate ??= today;
    }

    public void SetAutoStandbyEnabled(bool enabled)
    {
        lock (_autoSync)
        {
            if (_autoStandbyEnabled == enabled)
            {
                return;
            }

            _autoStandbyEnabled = enabled;
            if (enabled)
            {
                _autoStandbyCts = new CancellationTokenSource();
                _autoStandbyTask = Task.Run(() => AutoStandbyLoopAsync(_autoStandbyCts.Token));
                return;
            }

            var cts = _autoStandbyCts;
            _autoStandbyCts = null;
            _autoStandbyTask = null;
            try
            {
                cts?.Cancel();
            }
            catch
            {
            }
            finally
            {
                cts?.Dispose();
            }
        }

        _ = StopScanStandbyAsync();
    }

    [RelayCommand]
    private Task ReloadHistoryAsync()
        => RunSafeAsync(ct => LoadOcrHistoryAsync(ct));

    [RelayCommand]
    private Task ExportHistoryToExcelAsync()
        => RunSafeAsync(async ct =>
        {
            if (OcrHistory.Count == 0)
            {
                LastResult = "내보낼 히스토리가 없습니다.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"ocr-history-{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx",
                OverwritePrompt = true
            };

            var dialogResult = dialog.ShowDialog();
            if (dialogResult != true || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                LastResult = "엑셀 내보내기를 취소했습니다.";
                return;
            }

            var rows = OcrHistory
                .Select(x => new HistoryExcelRow(
                    x.TimestampUtc,
                    x.DocumentType,
                    x.DocumentNo,
                    x.Name,
                    x.Nationality))
                .ToList();

            var filePath = await _historyExcelExportService.ExportAsync(rows, dialog.FileName, ct);
            LastResult = $"엑셀 저장 완료: {filePath} ({rows.Count}건)";
        });

    [RelayCommand]
    private Task DeleteHistoryItemAsync(OcrHistoryItem? item)
        => RunSafeAsync(async ct =>
        {
            if (item is null)
            {
                LastResult = "삭제할 행이 없습니다.";
                return;
            }

            var deleted = await _ocrHistoryStore.DeleteByIdsAsync(new[] { item.Id }, ct);
            await LoadOcrHistoryAsync(ct);
            LastResult = deleted > 0
                ? $"행 삭제 완료: id={item.Id}"
                : "삭제할 행을 찾지 못했습니다.";
        });

    [RelayCommand]
    private Task DeleteSelectedHistoryAsync()
        => RunSafeAsync(async ct =>
        {
            var ids = OcrHistory
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                LastResult = "선택된 행이 없습니다.";
                return;
            }

            var deleted = await _ocrHistoryStore.DeleteByIdsAsync(ids, ct);
            await LoadOcrHistoryAsync(ct);
            LastResult = $"선택 삭제 완료: 요청 {ids.Length}건, 삭제 {deleted}건";
        });

    [RelayCommand]
    private Task SearchHistoryAsync()
        => RunSafeAsync(ct =>
        {
            ApplyHistoryFilter();
            LastResult = "검색 조건으로 히스토리를 조회했습니다.";
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task ClearHistoryFilterAsync()
        => RunSafeAsync(ct =>
        {
            SearchNo = string.Empty;
            SearchName = string.Empty;
            SearchStartDate = null;
            SearchEndDate = null;
            ApplyHistoryFilter();
            LastResult = "검색 조건을 초기화했습니다.";
            return Task.CompletedTask;
        });

    public async Task RefreshCoreAsync(CancellationToken cancellationToken = default)
    {
        var status = await _runtimePort.GetStatusAsync(DeviceId, cancellationToken);
        var connection = await _runtimePort.GetConnectionAsync(DeviceId, cancellationToken);
        if (status is null || connection is null)
        {
            DeviceType = "IDSCANNER";
            ConnectionState = DeviceConnectionState.Disconnected;
            IsHealthy = false;
            StatusMessage = "Device descriptor not found.";
            Timestamp = DateTimeOffset.UtcNow;
            return;
        }

        DeviceType = status.DeviceType;
        ConnectionState = connection.State;
        IsHealthy = status.IsHealthy;
        StatusMessage = ToConnectionMessage(connection.State);
        Timestamp = connection.Timestamp;
    }

    private static string ToConnectionMessage(DeviceConnectionState state)
        => state switch
        {
            DeviceConnectionState.Connected => "Connected",
            DeviceConnectionState.Connecting => "Connecting",
            DeviceConnectionState.Faulted => "Faulted",
            _ => "Disconnected"
        };

    private async Task GetScanStatusCoreAsync(CancellationToken cancellationToken)
    {
        var result = await _scanSessionService.PollOnceAsync(cancellationToken);
        await RefreshCoreAsync(cancellationToken);

        ApplyScanProgress(result);
        if (!result.Success)
        {
            LastResult = $"GETSCANSTATUS failed: {result.Code} {result.Message}";
            return;
        }

        LastResult = $"ScanStatus={ScanPresence}, detected={IsDetected}, polling={IsScanPolling}";
    }

    private void ResetDetectionState()
    {
        IsDetected = false;
        IsScanPolling = false;
        ScanPresence = "-";
        ClearOcrResult();
    }

    private async Task HandleDetectedAsync()
    {
        _awaitingEmptyAfterScan = true;
        _pauseAutoStandby = true;
        //ShowOperationOverlay("신분증 인식", "신분증 OCR을 진행합니다.");
        LastResult = "Detected. Executing SAVEIMAGE...";

        var captured = false;
        var ocrSuccess = false;

        try
        {
            captured = await SaveImageCoreAsync(CancellationToken.None);

            if (captured)
            {
                UpdateOperationOverlay("신분증 분석 중입니다.");
                ocrSuccess = await ExecuteOcrAsync(OcrMode.Auto, CancellationToken.None);
            }

            var title = "신분증 인식 실패";
            var message = "신분증을 제거해 주세요.";
            var success = false;

            if (!captured)
            {
                message = "신분증 인식을 실패했습니다. 문서를 제거해 주세요.";
            }
            else if (!ocrSuccess)
            {
                message = "신분증 분석을 실패했습니다. 문서를 제거해 주세요.";
            }
            else
            {
                title = "신분증 인식 완료";
                success = true;
            }

            LastResult = $"{LastResult}{Environment.NewLine}{message}";
            ShowProcessingResultOverlay(title, message, success);
        }
        catch (Exception ex)
        {
            var message = $"처리 중 오류가 발생했습니다. 문서를 제거해 주세요.{Environment.NewLine}{ex.Message}";
            LastResult = $"{LastResult}{Environment.NewLine}처리 오류: {ex.Message}";
            ShowProcessingResultOverlay("신분증 인식 실패", message, false);
        }
        finally
        {
            await RefreshCoreAsync(CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(1.2));
            _pauseAutoStandby = false;
        }
    }

    private async Task RunOcrCoreAsync(CancellationToken cancellationToken)
    {
        if (_lastCaptured is null)
        {
            var captured = await SaveImageCoreAsync(cancellationToken);
            if (!captured)
            {
                return;
            }
        }

        await ExecuteOcrAsync(OcrMode.Auto, cancellationToken);
    }

    private async Task<bool> SaveImageCoreAsync(CancellationToken cancellationToken)
    {
        var result = await _runtimePort.ExecuteAsync(DeviceId, new DeviceCommandRequest("SAVEIMAGE"), cancellationToken);
        await RefreshCoreAsync(cancellationToken);
        if (!result.Success)
        {
            LastResult = $"SAVEIMAGE failed: {result.Code} {result.Message}";
            return false;
        }

        if (!TryReadSaveImageResult(result, out var dto))
        {
            LastResult = "SAVEIMAGE succeeded but payload parsing failed.";
            return false;
        }

        _lastCaptured = dto;
        LastResult = $"SAVEIMAGE success.{Environment.NewLine}path={dto.ImagePath}";
        return true;
    }

    private bool TryReadSaveImageResult(DeviceCommandResponse result, out SaveImageResultDto dto)
    {
        dto = new SaveImageResultDto(string.Empty, Array.Empty<byte>());

        if (result.Data is SaveImageResultDto parsedByObject && parsedByObject.ImageByte is { Length: > 0 })
        {
            dto = parsedByObject;
            return true;
        }

        if (result.Data is not string payloadJson || string.IsNullOrWhiteSpace(payloadJson))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<SaveImageResultDto>(payloadJson);
            if (parsed is { ImageByte: { Length: > 0 } })
            {
                dto = parsed;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private async Task<bool> ExecuteOcrAsync(OcrMode mode, CancellationToken cancellationToken)
    {
        if (_lastCaptured is null)
        {
            LastResult = "No captured image/document bytes. Execute SAVEIMAGE first.";
            return false;
        }

        var ocrResult = await _ocrProcessingService.RunAsync(DeviceId, _lastCaptured, mode, cancellationToken);
        if (!ocrResult.Success)
        {
            ClearOcrResult();
            OcrStatus = $"OCR failed: {ocrResult.Source}";
            LastResult = $"OCR failed: {ocrResult.Error}";
            return false;
        }

        ocrResult = _ocrResultConverter.Normalize(ocrResult);

        if (ApplyOcrResult(ocrResult, out var fields))
        {
            var rawJson = JsonSerializer.Serialize(ocrResult);
            var deviceSerial = await ExtractCurrentSerialAsync(cancellationToken);
            var historySaved = await AppendOcrHistoryAsync(fields, ocrResult.DocumentType, deviceSerial, rawJson, cancellationToken);
            LastResult = $"OCR success.{Environment.NewLine}source={ocrResult.Source}, documentType={ocrResult.DocumentType ?? "-"}";
            if (historySaved)
            {
                await SyncPendingUsageAsync(cancellationToken);
            }
            else
            {
                LastResult = $"{LastResult}{Environment.NewLine}Usage API skipped: OCR DB 저장 실패.";
            }
            return true;
        }

        LastResult = "OCR succeeded but no fields found.";
        return false;
    }

    private bool ApplyOcrResult(RunOcrResultDto dto, out Dictionary<string, string> fields)
    {
        fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hasFields = dto.Fields is { Count: > 0 };
        var hasDocumentType = !string.IsNullOrWhiteSpace(dto.DocumentType);
        if (!hasFields && !hasDocumentType)
        {
            OcrFields.Clear();
            return false;
        }

        if (hasFields)
        {
            foreach (var item in dto.Fields!)
            {
                fields[item.Key] = item.Value ?? string.Empty;
            }
        }

        if (hasDocumentType)
        {
            fields["DOCUMENTTYPE"] = dto.DocumentType!;
        }

        ApplyFieldsToView(fields);

        OcrStatus = $"OCR success ({OcrFields.Count}), type={dto.DocumentType ?? "-"}";
        return true;
    }

    private void ClearOcrResult()
    {
        OcrFields.Clear();
        OcrStatus = "-";
    }

    private async Task<bool> AppendOcrHistoryAsync(
        IReadOnlyDictionary<string, string> fields,
        string? documentType,
        string? deviceSerial,
        string? rawJson,
        CancellationToken cancellationToken)
    {
        try
        {
            await _ocrHistoryStore.AddAsync(fields, documentType, deviceSerial, rawJson ?? string.Empty, cancellationToken);
            var rows = await _ocrHistoryStore.GetAllAsync(cancellationToken);
            _allHistoryRows = rows;
            ApplyHistoryFilter();
            OcrStatus = $"OCR success ({OcrFields.Count}), saved ({OcrHistory.Count})";
            return true;
        }
        catch (Exception ex)
        {
            LastResult = $"{LastResult}{Environment.NewLine}History save failed: {ex.Message}";
            return false;
        }
    }

    private async Task SyncPendingUsageAsync(CancellationToken cancellationToken)
    {
        if (!await _usageSyncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var apiKey = (await _apiKeyStore.LoadAsync(cancellationToken))?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                LastResult = $"{LastResult}{Environment.NewLine}Usage sync pending: API 키가 없습니다.";
                return;
            }

            var pendingRows = await _ocrHistoryStore.GetPendingUsageSyncRowsAsync(cancellationToken: cancellationToken);
            if (pendingRows.Count == 0)
            {
                return;
            }

            string? fallbackSerial = null;
            var successCount = 0;
            var failedCount = 0;

            foreach (var row in pendingRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var serial = row.DeviceSerial;
                if (string.IsNullOrWhiteSpace(serial))
                {
                    fallbackSerial ??= await ExtractCurrentSerialAsync(cancellationToken);
                    serial = fallbackSerial;
                }

                if (string.IsNullOrWhiteSpace(serial))
                {
                    await _ocrHistoryStore.MarkUsageSyncFailedAsync(
                        row.Id,
                        "장치 시리얼 추출 실패.",
                        row.UsageSyncLastResponse,
                        cancellationToken);
                    failedCount++;
                    continue;
                }

                var response = await _deviceApiClient.IncrementUsageAsync(serial, apiKey, cancellationToken);
                if (response.Success)
                {
                    await _ocrHistoryStore.MarkUsageSyncSucceededAsync(
                        row.Id,
                        response.DateKey,
                        response.TotalUsage,
                        response.RawBody,
                        cancellationToken);
                    successCount++;
                    continue;
                }

                await _ocrHistoryStore.MarkUsageSyncFailedAsync(
                    row.Id,
                    response.Message,
                    response.RawBody,
                    cancellationToken);
                failedCount++;
            }

            if (successCount > 0 || failedCount > 0)
            {
                LastResult = $"{LastResult}{Environment.NewLine}Usage sync summary: success={successCount}, failed={failedCount}";
            }
        }
        catch (Exception ex)
        {
            LastResult = $"{LastResult}{Environment.NewLine}Usage sync error: {ex.Message}";
        }
        finally
        {
            _usageSyncLock.Release();
        }
    }

    private async Task<string?> ExtractCurrentSerialAsync(CancellationToken cancellationToken)
    {
        var result = await _runtimePort.ExecuteAsync(DeviceId, new DeviceCommandRequest("GETDEVICEID"), cancellationToken);
        if (!result.Success)
        {
            return null;
        }

        return ToRegistrationSerial(result.Data?.ToString());
    }

    private static string? ToRegistrationSerial(string? rawSerial)
    {
        var normalized = (rawSerial ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());
        var baseSerial = digitsOnly.Length >= 7
            ? digitsOnly[^7..]
            : normalized.Length >= 7
                ? normalized[^7..]
                : normalized;

        if (string.IsNullOrWhiteSpace(baseSerial))
        {
            return null;
        }

        var chars = baseSerial.ToCharArray();
        chars[0] = '1';
        return new string(chars);
    }

    private async Task LoadOcrHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _ocrHistoryStore.GetAllAsync(cancellationToken);
            _allHistoryRows = rows;
            ApplyHistoryFilter();
        }
        catch
        {
        }
    }

    private async Task InitializeAsync()
    {
        await LoadOcrHistoryAsync(CancellationToken.None);

        try
        {
            await SyncPendingUsageAsync(CancellationToken.None);
        }
        catch
        {
        }
    }

    private void ApplyHistoryFilter()
    {
        var fromDate = SearchStartDate?.Date;
        var toDate = SearchEndDate?.Date;
        var noKeyword = (SearchNo ?? string.Empty).Trim();
        var nameKeyword = (SearchName ?? string.Empty).Trim();

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        var filtered = _allHistoryRows
            .Where(row =>
            {
                var localDate = row.TimestampUtc.ToLocalTime().Date;
                var afterFrom = !fromDate.HasValue || localDate >= fromDate.Value;
                var beforeTo = !toDate.HasValue || localDate <= toDate.Value;
                var matchNo = string.IsNullOrWhiteSpace(noKeyword) ||
                              row.DocumentNo.Contains(noKeyword, StringComparison.OrdinalIgnoreCase);
                var matchName = string.IsNullOrWhiteSpace(nameKeyword) ||
                                row.Name.Contains(nameKeyword, StringComparison.OrdinalIgnoreCase);
                return afterFrom && beforeTo && matchNo && matchName;
            })
            .ToList();

        ReplaceOcrHistory(filtered);
    }

    private void ReplaceOcrHistory(IReadOnlyList<OcrHistoryRow> rows)
    {
        foreach (var existing in OcrHistory)
        {
            existing.PropertyChanged -= OnHistoryItemPropertyChanged;
        }

        OcrHistory.Clear();
        foreach (var row in rows)
        {
            var item = new OcrHistoryItem(
                row.Id,
                row.TimestampUtc,
                row.DocumentType,
                row.DocumentNo,
                row.Name,
                row.Nationality,
                row.BirthDate,
                row.ExpiryDate,
                row.RawJson);
            item.PropertyChanged += OnHistoryItemPropertyChanged;
            OcrHistory.Add(item);
        }

        _suspendSelectAllSync = true;
        IsAllHistorySelected = false;
        _suspendSelectAllSync = false;

        SelectedOcrHistory = OcrHistory.FirstOrDefault();
        if (SelectedOcrHistory is null)
        {
            OcrFields.Clear();
            OcrStatus = "-";
        }
    }

    private void OnHistoryItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(OcrHistoryItem.IsSelected), StringComparison.Ordinal))
        {
            return;
        }

        var allSelected = OcrHistory.Count > 0 && OcrHistory.All(x => x.IsSelected);
        if (IsAllHistorySelected == allSelected)
        {
            return;
        }

        _suspendSelectAllSync = true;
        IsAllHistorySelected = allSelected;
        _suspendSelectAllSync = false;
    }

    public async Task HandleConnectionLostAsync()
    {
        CancelActiveOperation();
        SetAutoStandbyEnabled(false);
        _pauseAutoStandby = false;
        HideOperationOverlay();

        try
        {
            await _scanSessionService.StopAsync(CancellationToken.None);
        }
        catch
        {
        }

        ResetDetectionState();
        await RefreshCoreAsync(CancellationToken.None);
        LastResult = "Device disconnected. Scan session stopped and active operation cancelled.";
    }

    private void CancelActiveOperation()
    {
        lock (_operationSync)
        {
            try
            {
                _activeOperationCts?.Cancel();
            }
            catch
            {
            }
        }
    }

    private Task RunSafeAsync(Func<Task> action)
        => RunSafeAsync(_ => action());

    private async Task RunSafeAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        CancellationTokenSource? cts = null;
        try
        {
            lock (_operationSync)
            {
                cts = new CancellationTokenSource();
                _activeOperationCts = cts;
            }

            IsBusy = true;
            await action(cts.Token);
        }
        catch (OperationCanceledException)
        {
            LastResult = "Operation cancelled.";
        }
        catch (Exception ex)
        {
            LastResult = $"ERROR: {ex.Message}";
        }
        finally
        {
            lock (_operationSync)
            {
                if (ReferenceEquals(_activeOperationCts, cts))
                {
                    _activeOperationCts = null;
                }
            }

            cts?.Dispose();
            IsBusy = false;
        }
    }

    private void OnScanSessionProgressChanged(object? sender, ScanSessionProgress progress)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            _ = HandleScanProgressAsync(progress);
            return;
        }

        _ = dispatcher.InvokeAsync(async () => await HandleScanProgressAsync(progress));
    }

    private async Task HandleScanProgressAsync(ScanSessionProgress progress)
    {
        ApplyScanProgress(progress);
        UpdateScanOverlayByPresence(progress);
        if (!progress.Success)
        {
            LastResult = $"GETSCANSTATUS failed: {progress.Code} {progress.Message}";
            return;
        }

        if (!progress.IsDetected || _detectedHandling)
        {
            return;
        }

        _detectedHandling = true;
        try
        {
            await HandleDetectedAsync();
        }
        finally
        {
            _detectedHandling = false;
        }
    }

    private void ApplyScanProgress(ScanSessionProgress progress)
    {
        ScanPresence = progress.Presence;
        IsDetected = progress.IsDetected;
        IsScanPolling = progress.IsPolling;
    }

    private async Task AutoStandbyLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await AutoStandbyTickAsync(cancellationToken);
                await Task.Delay(AutoStandbyTick, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task AutoStandbyTickAsync(CancellationToken cancellationToken)
    {
        await RefreshCoreAsync(cancellationToken);

        if (ConnectionState != DeviceConnectionState.Connected || !IsHealthy)
        {
            return;
        }

        if (_detectedHandling || _pauseAutoStandby)
        {
            return;
        }

        if (_awaitingEmptyAfterScan)
        {
            var progress = await _scanSessionService.PollOnceAsync(cancellationToken);
            ApplyScanProgress(progress);
            if (!progress.Success)
            {
                LastResult = $"문서 제거 확인 실패: {progress.Code} {progress.Message}";
                return;
            }

            if (!string.Equals(progress.Presence, "EMPTY", StringComparison.OrdinalIgnoreCase))
            {
                if (_appOverlayService.Current.IsVisible)
                {
                    _appOverlayService.UpdateProgressMessage($"신분증을 제거 해주세요.");
                }

                LastResult = $"문서 제거 대기 중: {progress.Presence}";
                return;
            }

            _awaitingEmptyAfterScan = false;
            HideOperationOverlay();
            IsDetected = false;
            LastResult = "문서 제거 확인(EMPTY). 다음 스캔을 시작합니다.";
        }

        if (IsScanPolling)
        {
            return;
        }

        var result = await _scanSessionService.StartAsync(cancellationToken);
        if (result.Success || string.Equals(result.Code?.Detail, "ALREADY_POLLING", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsScanPolling)
            {
                IsScanPolling = true;
            }

            LastResult = "자동 대기: 스캔 상태 폴링 중";
            return;
        }

        LastResult = $"자동 대기 시작 실패: {result.Code} {result.Message}";
    }

    private async Task StopScanStandbyAsync()
    {
        try
        {
            await _scanSessionService.StopAsync(CancellationToken.None);
        }
        catch
        {
        }

        IsScanPolling = false;
    }

    private void ShowOperationOverlay(string title, string message)
    {
        _appOverlayService.ShowProgress(title, message);
    }

    private void UpdateOperationOverlay(string message)
    {
        _appOverlayService.UpdateProgressMessage(message);
    }

    private void ShowProcessingResultOverlay(string title, string message, bool success)
    {
        _appOverlayService.ShowResult(title, message, success);
    }

    private void HideOperationOverlay()
    {
        _appOverlayService.Hide();
    }

    private void UpdateScanOverlayByPresence(ScanSessionProgress progress)
    {
        if (!progress.Success)
        {
            return;
        }

        if (_awaitingEmptyAfterScan || _detectedHandling || _pauseAutoStandby)
        {
            return;
        }

        var presence = progress.Presence?.ToUpperInvariant() ?? string.Empty;
        switch (presence)
        {
            case "EMPTY":
                HideOperationOverlay();
                break;
            case "PRESENT":
            case "MOVING":
                ShowOperationOverlay("신분증 인식", "신분증을 인식 중입니다.");
                break;
            case "NOMOVE":
                ShowOperationOverlay("신분증 인식", "신분증을 멈춘 상태로 유지해 주세요.");
                break;
        }
    }

    partial void OnSelectedOcrHistoryChanged(OcrHistoryItem? value)
    {
        ApplyHistoryToOcrFields(value);
    }

    private void ApplyHistoryToOcrFields(OcrHistoryItem? item)
    {
        if (item is null)
        {
            OcrFields.Clear();
            OcrStatus = "-";
            return;
        }

        var fields = new Dictionary<string, string>(TryParseFieldsFromRawJson(item.RawJson), StringComparer.OrdinalIgnoreCase);
        if (fields.Count == 0)
        {
            fields["DOCUMENTTYPE"] = item.DocumentType;
            fields["NO"] = item.DocumentNo;
            fields["NAME"] = item.Name;
            fields["NATIONALITY"] = item.Nationality;
            fields["BIRTHDATE"] = item.BirthDate;
            fields["EXPIRYDATE"] = item.ExpiryDate;
        }

        ApplyFieldsToView(fields);
        OcrStatus = $"DB row selected ({OcrFields.Count})";
    }

    private static IReadOnlyDictionary<string, string> TryParseFieldsFromRawJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var dto = JsonSerializer.Deserialize<RunOcrResultDto>(rawJson);
            if (dto?.Fields is { Count: > 0 })
            {
                var fields = new Dictionary<string, string>(dto.Fields, StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(dto.DocumentType))
                {
                    fields["DOCUMENTTYPE"] = dto.DocumentType;
                }

                return fields;
            }
        }
        catch
        {
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private void ApplyFieldsToView(IReadOnlyDictionary<string, string> fields)
    {
        OcrFields.Clear();
        foreach (var item in fields.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            OcrFields.Add(new OcrFieldItem(item.Key, item.Value ?? string.Empty));
        }
    }

    private static string FormatCode(DeviceCommandResponse result)
        => result.Code?.ToString() ?? (result.Success ? "OK" : "FAILED");

    public sealed record OcrFieldItem(string Key, string Value);

    public sealed partial class OcrHistoryItem : ObservableObject
    {
        public OcrHistoryItem(
            long id,
            DateTimeOffset timestampUtc,
            string documentType,
            string documentNo,
            string name,
            string nationality,
            string birthDate,
            string expiryDate,
            string rawJson)
        {
            Id = id;
            TimestampUtc = timestampUtc;
            DocumentType = documentType;
            DocumentNo = documentNo;
            Name = name;
            Nationality = nationality;
            BirthDate = birthDate;
            ExpiryDate = expiryDate;
            RawJson = rawJson;
        }

        public long Id { get; }
        public DateTimeOffset TimestampUtc { get; }
        public string DocumentType { get; }
        public string DocumentNo { get; }
        public string Name { get; }
        public string Nationality { get; }
        public string BirthDate { get; }
        public string ExpiryDate { get; }
        public string RawJson { get; }

        [ObservableProperty]
        private bool isSelected;
    }
}
