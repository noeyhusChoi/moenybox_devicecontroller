using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KIOSK.Device.Abstractions;
using KIOSK.Device.Transport;
using Pr22;
using Pr22.Events;
using Pr22.Imaging;
using Pr22.Processing;
using Pr22.Task;
using Path = System.IO.Path;

namespace KIOSK.Device.Drivers.IdScanner;

/// <summary>
/// PR22 DLL 기반 신분증 스캐너 클라이언트.
/// TransportPr22가 제공하는 DocumentReaderDevice를 사용해 스캔/저장/상태 조회를 수행한다.
/// </summary>
internal sealed class IdScannerClient : IAsyncDisposable
{
    private readonly TransportPr22 _transport;
    private DocumentReaderDevice? _device;
    private Pr22.Util.PresenceState _presenceState = Pr22.Util.PresenceState.Empty;
    private Pr22.Util.PresenceState _lastPresenceState = Pr22.Util.PresenceState.Empty;
    private readonly object _presenceLock = new();
    private bool _presenceSubscribed;
    private bool _detectedRaised;
    private CancellationTokenSource? _noMoveHoldCts;

    public event Action<string>? Log;
    public event EventHandler<IdScannerScanEvent>? ScanSequence;
    public event EventHandler<(int page, Light light, string path)>? ImageSaved;
    public event EventHandler? Detected;

    public IdScannerClient(TransportPr22 transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_device is not null)
            return;

        await _transport.OpenAsync(ct).ConfigureAwait(false);
        _device = _transport.Device;
        Log?.Invoke($"[IDSCANNER] Device connected: {_device.DeviceName}");
    }

    public async Task<CommandResult> GetStatusAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);

        try
        {
            var info = _device!.Scanner.Info;
            info.IsCalibrated();
            return new CommandResult(true, Data: IdScannerState.Ready);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[IDSCANNER] 상태 조회 실패: {ex.Message}");
            return new CommandResult(false, string.Empty, IdScannerState.Error, new ErrorCode("DEV", "IDSCANNER", "STATUS", "ERROR"));
        }
    }

    public async Task<CommandResult> StartScanAsync(CancellationToken ct = default)
    {
        try
        {
            await StartAsync(ct).ConfigureAwait(false);
            var device = RequireDevice();

            _detectedRaised = false;
            _lastPresenceState = Pr22.Util.PresenceState.Empty;
            CancelNoMoveHold();

            lock (_presenceLock)
            {
                if (!_presenceSubscribed)
                {
                    device.PresenceStateChanged += OnPresence;
                    _presenceSubscribed = true;
                }
            }
            device.Scanner.StartTask(FreerunTask.Detection());
            ScanSequence?.Invoke(this, IdScannerScanEvent.Scanning);
            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[IDSCANNER] ScanStart 실패: {ex.Message}");
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "ERROR", "SCAN"));
        }
    }

    public async Task<CommandResult> StopScanAsync(CancellationToken ct = default)
    {
        try
        {
            await StartAsync(ct).ConfigureAwait(false);
            var device = RequireDevice();

            CancelNoMoveHold();
            _detectedRaised = false;

            lock (_presenceLock)
            {
                if (_presenceSubscribed)
                {
                    device.PresenceStateChanged -= OnPresence;
                    _presenceSubscribed = false;
                }
            }

            return new CommandResult(true);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[IDSCANNER] ScanStop 실패: {ex.Message}");
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "ERROR", "SCAN"));
        }
    }

    public async Task<CommandResult> GetPresenceAsync(CancellationToken ct = default)
    {
        await StartAsync(ct).ConfigureAwait(false);
        return new CommandResult(true, Data: _presenceState);
    }

    public async Task<CommandResult> SaveImageAsync(CancellationToken ct = default)
    {
        try
        {
            await StartAsync(ct).ConfigureAwait(false);
            var device = RequireDevice();
            var task = new DocScannerTask();
            task.Add(Light.White).Add(Light.Infra);

            var page = device.Scanner.Scan(task, PagePosition.First);

            var saveDir = Path.Combine(Environment.CurrentDirectory, "ScanOutput");
            Directory.CreateDirectory(saveDir);

            try
            {
                var img = page.Select(Light.White).GetImage();
                var whitePath = Path.Combine(saveDir, $"scan_{Light.White}.jpg");
                img.Save(RawImage.FileFormat.Jpeg).Save(whitePath);
                ImageSaved?.Invoke(this, (1, Light.White, whitePath));

                img = page.Select(Light.Infra).GetImage();
                var infraPath = Path.Combine(saveDir, $"scan_{Light.Infra}.jpg");
                img.Save(RawImage.FileFormat.Jpeg).Save(infraPath);
                ImageSaved?.Invoke(this, (1, Light.Infra, infraPath));

                ScanSequence?.Invoke(this, IdScannerScanEvent.ScanComplete);
                return new CommandResult(true, Data: page);
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[IDSCANNER] 이미지 저장 실패: {ex.Message}");
                return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "ERROR", "IMAGE_SAVE"));
            }
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[IDSCANNER] Scan/Save 처리 중 예외: {ex.Message}");
            return new CommandResult(false, string.Empty, Code: new ErrorCode("DEV", "IDSCANNER", "ERROR", "IMAGE_SAVE"));
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
            Trace.WriteLine($"{e.State}");
            _presenceState = e.State;
            if (e.State == Pr22.Util.PresenceState.NoMove)
            {
                    StartNoMoveHold();
            }
            else
            {
                CancelNoMoveHold();
                _detectedRaised = false;
            }

            var ev = e.State switch
            {
                Pr22.Util.PresenceState.Empty => IdScannerScanEvent.Empty,
                Pr22.Util.PresenceState.Moving => IdScannerScanEvent.Scanning,
                Pr22.Util.PresenceState.Present => IdScannerScanEvent.Scanning,
                Pr22.Util.PresenceState.NoMove => IdScannerScanEvent.ScanComplete,
                _ => IdScannerScanEvent.Empty
            };
            ScanSequence?.Invoke(this, ev);

            _lastPresenceState = e.State;
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[IDSCANNER] Presence 처리 예외: {ex.Message}");
        }
    }

    private DocumentReaderDevice RequireDevice()
        => _device ?? throw new InvalidOperationException("PR22 기기가 초기화되지 않았습니다.");

    private void StartNoMoveHold()
    {
        if (_detectedRaised)
            return;

        CancelNoMoveHold();
        _noMoveHoldCts = new CancellationTokenSource();
        var token = _noMoveHoldCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(IdScannerDefaults.NoMoveHoldMs, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested &&
                    _presenceState == Pr22.Util.PresenceState.NoMove &&
                    !_detectedRaised)
                {
                    _detectedRaised = true;
                    Detected?.Invoke(this, EventArgs.Empty);
                    Trace.WriteLine("DETECTED");
                    _ = StopScanAsync(CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[IDSCANNER] NoMove hold error: {ex.Message}");
            }
        }, token);
    }

    private void CancelNoMoveHold()
    {
        try
        {
            _noMoveHoldCts?.Cancel();
            _noMoveHoldCts?.Dispose();
        }
        catch
        {
        }
        finally
        {
            _noMoveHoldCts = null;
        }
    }

}
