using DeviceKit.Commands.IdScanner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pr22;
using Pr22.Events;
using Pr22.Imaging;
using Pr22.Processing;
using Pr22.Task;
using System.IO.Compression;
using System.Text.Json;
using Path = System.IO.Path;

namespace DeviceKit.Drivers.IdScanner;

/// <summary>
/// PR22 DLL 기반 신분증 스캐너 클라이언트.
/// TransportPr22가 제공하는 DocumentReaderDevice를 사용해 스캔/저장/상태 조회를 수행한다.
/// </summary>
internal sealed class IdScannerClient : IAsyncDisposable
{
    private readonly TransportPr22 _transport;
    private readonly ILogger _logger;
    private DocumentReaderDevice? _device;
    private Pr22.Util.PresenceState _presenceState = Pr22.Util.PresenceState.Empty;
    private Pr22.Util.PresenceState _lastPresenceState = Pr22.Util.PresenceState.Empty;
    private readonly object _presenceLock = new();
    private bool _presenceSubscribed;

    public event EventHandler<(int page, Light light, string path)>? ImageSaved;
    public event Action? DocumentDetected;
    public event EventHandler<IdScannerScanStatus>? ScanStatusChanged;

    public IdScannerClient(DeviceDescriptor descriptor, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _transport = new TransportPr22();
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_device is not null)
            return;

        await _transport.OpenAsync(ct).ConfigureAwait(false);
        _device = _transport.Device;
        _logger.LogInformation("IdScanner device connected. device={DeviceName}", _device.DeviceName);
    }

    public async Task<DeviceCommandResponse> GetStatusAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);

        var list = DocumentReaderDevice.GetDeviceList();
        if (list.Count == 0)
            throw new Pr22.Exceptions.NoSuchDevice("No device found.");

        var info = _device!.Scanner.Info;
        info.IsCalibrated();
        return new DeviceCommandResponse(true, Data: IdScannerState.Ready);
    }

    public async Task<DeviceCommandResponse> GetDeviceIdAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);

        var deviceList = DocumentReaderDevice.GetDeviceList();
        var deviceId = ResolveDeviceId(RequireDevice(), deviceList);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "DEVICE_ID", "NOT_FOUND"));
        }

        return new DeviceCommandResponse(true, Data: deviceId);
    }

    public async Task<DeviceCommandResponse> StartScanAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);
        var device = RequireDevice();

        // _lastPresenceState = Pr22.Util.PresenceState.Empty;

        lock (_presenceLock)
        {
            if (!_presenceSubscribed)
            {
                device.PresenceStateChanged += OnPresence;

                //device.ScanStarted += (s, e) =>
                //{
                //    Trace.WriteLine("[ScanStarted] ====> Scan started.");
                //};
                //device.ScanFinished += (s, e) =>
                //{
                //    Trace.WriteLine("[ScanFinished] ====> Scan finished.");
                //};
                //device.DocFrameFound += (s, e) =>
                //{
                //    Trace.WriteLine("[DocFrameFound] ====> Document frame found.");
                //};

                _presenceSubscribed = true;
            }
        }
        device.Scanner.StartTask(FreerunTask.Detection());
        return new DeviceCommandResponse(true);
    }

    public async Task<DeviceCommandResponse> StopScanAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);
        var device = RequireDevice();


        lock (_presenceLock)
        {
            if (_presenceSubscribed)
            {
                device.PresenceStateChanged -= OnPresence;
                _presenceSubscribed = false;
            }
        }

        return new DeviceCommandResponse(true);
    }

    public async Task<DeviceCommandResponse> GetPresenceAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);
        return new DeviceCommandResponse(true, Data: _presenceState);
    }

    public async Task<DeviceCommandResponse> SaveImageAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);
        var device = RequireDevice();
        ct.ThrowIfCancellationRequested();

        try
        {
            var dto = await Task.Run(() => CaptureDocumentSync(device, ct), ct).ConfigureAwait(false);
            return new DeviceCommandResponse(true, Data: dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Pr22.Exceptions.ImageProcessingFailed ex)
        {
            _logger.LogWarning(ex, "IdScanner SaveImage image processing failed.");
            return new DeviceCommandResponse(false, ex.Message, Code: new ErrorCode("DEV", "IDSCANNER", "SAVE_IMAGE", "IMAGE_PROCESSING_FAILED"));
        }
        catch (Pr22.Exceptions.General ex)
        {
            _logger.LogWarning(ex, "IdScanner SaveImage PR22 failed.");
            return new DeviceCommandResponse(false, ex.Message, Code: new ErrorCode("DEV", "IDSCANNER", "SAVE_IMAGE", "PR22_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IdScanner SaveImage failed.");
            return new DeviceCommandResponse(false, ex.Message, Code: new ErrorCode("DEV", "IDSCANNER", "SAVE_IMAGE", "FAILED"));
        }
    }

    public async Task<DeviceCommandResponse> RunOcrAsync(string? payload, CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);
        var device = RequireDevice();

        try
        {
            ct.ThrowIfCancellationRequested();
            var dto = await Task.Run(() => RunOcrSync(device, payload, ct), ct).ConfigureAwait(false);
            if (!dto.Success)
            {
                return new DeviceCommandResponse(false, Message: dto.Error ?? "No MRZ fields detected.", Code: new ErrorCode("DEV", "IDSCANNER", "OCR", "NO_FIELDS"));
            }

            return new DeviceCommandResponse(true, Data: dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DeviceCommandResponse(false, Message: ex.Message, Code: new ErrorCode("DEV", "IDSCANNER", "OCR", "FAILED"));
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_device is not null)
            {
                try { _device.Close(); } catch { }
                try { _device.Dispose(); } catch { }
            }
        }
        catch { }

        try { await _transport.DisposeAsync().ConfigureAwait(false); } catch { }

        _device = null;
    }

    private void OnPresence(object? sender, DetectionEventArgs e)
    {
        try
        {
            _logger.LogDebug("IdScanner presence state changed. state={State}", e.State);
            _presenceState = e.State;

            var status = MapPresenceState(e.State);
            ScanStatusChanged?.Invoke(this, status);

            if (_lastPresenceState == Pr22.Util.PresenceState.Empty && e.State != Pr22.Util.PresenceState.Empty)
                DocumentDetected?.Invoke();

            _lastPresenceState = e.State;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IdScanner presence handling failed.");
        }
    }

    private static IdScannerScanStatus MapPresenceState(Pr22.Util.PresenceState state)
        => state switch
        {
            Pr22.Util.PresenceState.Empty => IdScannerScanStatus.Empty,
            Pr22.Util.PresenceState.Moving => IdScannerScanStatus.Moving,
            Pr22.Util.PresenceState.NoMove => IdScannerScanStatus.NoMove,
            Pr22.Util.PresenceState.Dirty => IdScannerScanStatus.Dirty,
            Pr22.Util.PresenceState.Present => IdScannerScanStatus.Present,
            Pr22.Util.PresenceState.Preparing => IdScannerScanStatus.Preparing,
            _ => IdScannerScanStatus.Empty
        };

    private DocumentReaderDevice RequireDevice()
        => _device ?? throw new InvalidOperationException("PR22 기기가 초기화되지 않았습니다.");

    private static string ResolveDeviceId(DocumentReaderDevice device, System.Collections.Generic.List<string> deviceList)
    {
        if (!string.IsNullOrWhiteSpace(device.DeviceName))
            return device.DeviceName;

        return "unknown";
    }

    private static byte[] ResolveDocumentBytes(string? payload, DocumentReaderDevice device)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                var dto = JsonSerializer.Deserialize<SaveImageResultDto>(payload);
                if (dto?.ImageByte is { Length: > 0 })
                {
                    return dto.ImageByte;
                }
            }
            catch
            {
                // fallback to current device root document
            }
        }

        var root = device.Engine.GetRootDocument();
        return root.Save(Document.FileFormat.Zipped).ToByteArray();
    }

    private static Dictionary<string, string> BuildMrzResultFields(Document analyze)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddField(result, "NAME", GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.Name));
        AddField(result, "GIVENNAME", GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.Givenname));
        AddField(result, "SURNAME", GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.Surname));
        AddField(result, "NO", GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.DocumentNumber));
        AddField(result, "NATIONALITY", GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.Nationality));
        AddField(result, "BIRTHDATE", GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.BirthDate));
        AddField(result, "EXPIRYDATE", GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.ExpiryDate));

        return result;
    }

    private SaveImageResultDto CaptureDocumentSync(DocumentReaderDevice device, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var task = new DocScannerTask();
        task.Add(Light.White).Add(Light.Infra);
        var page = device.Scanner.Scan(task, PagePosition.First);

        var root = device.Engine.GetRootDocument();
        var docBytes = root.Save(Document.FileFormat.Zipped).ToByteArray();

        var saveDir = Path.Combine(Environment.CurrentDirectory, "ScanOutput");
        Directory.CreateDirectory(saveDir);

        var img = page.Select(Light.White).GetImage();
        var whitePath = Path.Combine(saveDir, $"scan_{Light.White}.jpg");
        img.Save(RawImage.FileFormat.Jpeg).Save(whitePath);
        ImageSaved?.Invoke(this, (1, Light.White, whitePath));

        try
        {
            img = page.Select(Light.Infra).DocView().GetImage();
        }
        catch (Pr22.Exceptions.ImageProcessingFailed ex)
        {
            // 문서 영역 검출 실패 시 전체 IR 이미지를 사용한다.
            _logger.LogWarning(ex, "IdScanner infra DocView fallback.");
            img = page.Select(Light.Infra).GetImage();
        }
        var infraPath = Path.Combine(saveDir, $"scan_{Light.Infra}.jpg");
        img.Save(RawImage.FileFormat.Jpeg).Save(infraPath);
        ImageSaved?.Invoke(this, (1, Light.Infra, infraPath));

        return new SaveImageResultDto(
            ImagePath: infraPath,
            ImageByte: docBytes,
            WhiteImagePath: whitePath
        );
    }

    private RunOcrResultDto RunOcrSync(DocumentReaderDevice device, string? payload, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var docBytes = ResolveDocumentBytes(payload, device);
        device.Scanner.LoadDocument(new BinData(docBytes));
        var page = device.Scanner.GetPage(0);

        var task = new EngineTask();
        task.Add(FieldSource.Mrz, FieldId.All);

        var analyze = device.Engine.Analyze(page, task);
        var fields = BuildMrzResultFields(analyze);
        if (fields.Count == 0)
        {
            return new RunOcrResultDto(
                Success: false,
                Source: "MRZ",
                DocumentType: null,
                Fields: fields,
                Error: "No MRZ fields detected.");
        }

        // 강제 문서 유형 설정 (여권으로 고정)
        const string internalMrzDocumentType = "01";
        fields["DOCUMENTTYPE"] = internalMrzDocumentType;

        return new RunOcrResultDto(
            Success: true,
            Source: "MRZ",
            DocumentType: internalMrzDocumentType,
            Fields: fields,
            Error: null);
    }

    private static string? ResolveDocumentType(Document analyze, IDictionary<string, string> fields)
    {
        var docType = GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.DocType);
        if (string.IsNullOrWhiteSpace(docType))
        {
            docType = GetFieldValueSafe(analyze, FieldSource.Mrz, FieldId.DocTypeDisc);
        }

        if (string.IsNullOrWhiteSpace(docType))
        {
            docType = GetFieldValueSafe(analyze, FieldSource.Viz, FieldId.DocType);
        }

        if (string.IsNullOrWhiteSpace(docType))
        {
            docType = GetFieldValueSafe(analyze, FieldSource.Viz, FieldId.DocTypeDisc);
        }

        if (!string.IsNullOrWhiteSpace(docType))
        {
            var normalized = docType.Trim();
            fields["DOCUMENTTYPE"] = normalized;
            return normalized;
        }

        return null;
    }

    private static string? GetFieldValueSafe(Document analyze, FieldSource source, FieldId fieldId)
    {
        try
        {
            return analyze.GetField(source, fieldId)?.GetBestStringValue();
        }
        catch (Pr22.Exceptions.EntryNotFound)
        {
            return null;
        }
        catch (Pr22.Exceptions.General)
        {
            return null;
        }
    }

    private static void AddField(Dictionary<string, string> map, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            map[key] = value.Trim();
        }
    }

    private static byte[] BuildPagePayloadBytes(string whitePath, string infraPath)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var whiteEntry = zip.CreateEntry(Path.GetFileName(whitePath));
            using (var entryStream = whiteEntry.Open())
            using (var fileStream = File.OpenRead(whitePath))
            {
                fileStream.CopyTo(entryStream);
            }

            var infraEntry = zip.CreateEntry(Path.GetFileName(infraPath));
            using (var entryStream = infraEntry.Open())
            using (var fileStream = File.OpenRead(infraPath))
            {
                fileStream.CopyTo(entryStream);
            }
        }

        return ms.ToArray();
    }

}
