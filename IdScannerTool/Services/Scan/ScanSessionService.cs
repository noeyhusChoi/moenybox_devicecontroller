
namespace IdScannerTool.Services;

public sealed record ScanSessionProgress(
    string Presence,
    bool IsDetected,
    bool IsPolling,
    bool Success,
    string? Code = null,
    string? Message = null);

/// <summary>
/// 스캔 세션의 상태 폴링, NoMove hold 감지, 자동 진행 이벤트를 담당한다.
/// </summary>
public sealed class ScanSessionService : IScanSessionService
{
    private const int NoMoveHoldMs = 500;
    private const int PollingMs = 200;

    private readonly IDeviceManagerPort _runtimePort;
    private readonly string _deviceId;
    private readonly object _sync = new();

    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private DateTimeOffset? _noMoveSinceUtc;
    private bool _isDetected;
    private bool _isPolling;

    public ScanSessionService(IDeviceManagerPort runtimePort, string deviceId)
    {
        _runtimePort = runtimePort;
        _deviceId = deviceId;
    }

    public event EventHandler<ScanSessionProgress>? ProgressChanged;

    public async Task<DeviceCommandResponse> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_isPolling)
            {
                return new DeviceCommandResponse(
                    Success: true,
                    Message: "Scan polling is already running.",
                    Data: "Already polling.",
                    Code: new ErrorCode("SYS", "APP", "STATE", "ALREADY_POLLING"));
            }

            _noMoveSinceUtc = null;
            _isDetected = false;
        }

        var startResult = await _runtimePort.ExecuteAsync(_deviceId, new DeviceCommandRequest("SCANSTART"), cancellationToken);
        if (!startResult.Success)
        {
            return startResult;
        }

        CancellationTokenSource cts;
        lock (_sync)
        {
            _pollingCts = new CancellationTokenSource();
            cts = _pollingCts;
            _isPolling = true;
        }

        _pollingTask = Task.Run(() => PollLoopAsync(cts.Token), cts.Token);
        return startResult;
    }

    public async Task<DeviceCommandResponse> StopAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? cts;
        Task? pollingTask;
        lock (_sync)
        {
            cts = _pollingCts;
            pollingTask = _pollingTask;
            _pollingCts = null;
            _pollingTask = null;
            _isPolling = false;
            _noMoveSinceUtc = null;
            _isDetected = false;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        if (pollingTask is not null)
        {
            try
            {
                await pollingTask;
            }
            catch
            {
            }
        }

        return await _runtimePort.ExecuteAsync(_deviceId, new DeviceCommandRequest("SCANSTOP"), cancellationToken);
    }

    public async Task<ScanSessionProgress> PollOnceAsync(CancellationToken cancellationToken = default)
    {
        var result = await _runtimePort.ExecuteAsync(_deviceId, new DeviceCommandRequest("GETSCANSTATUS"), cancellationToken);
        if (!result.Success)
        {
            return new ScanSessionProgress(
                Presence: "ERROR",
                IsDetected: false,
                IsPolling: GetIsPolling(),
                Success: false,
                Code: result.Code?.ToString(),
                Message: result.Message);
        }

        var presence = NormalizePresence(result.Data?.ToString());
        var detected = UpdateDetectedState(presence);
        return new ScanSessionProgress(
            Presence: presence,
            IsDetected: detected,
            IsPolling: GetIsPolling(),
            Success: true);
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var progress = await PollOnceAsync(cancellationToken);
                ProgressChanged?.Invoke(this, progress);

                if (progress.IsDetected)
                {
                    lock (_sync)
                    {
                        _isPolling = false;
                    }

                    ProgressChanged?.Invoke(this, progress with { IsPolling = false });
                    break;
                }

                await Task.Delay(PollingMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            ProgressChanged?.Invoke(this, new ScanSessionProgress(
                Presence: "ERROR",
                IsDetected: false,
                IsPolling: false,
                Success: false,
                Code: "POLLING_ERROR",
                Message: ex.Message));
        }
        finally
        {
            lock (_sync)
            {
                _isPolling = false;
            }
        }
    }

    private bool GetIsPolling()
    {
        lock (_sync)
        {
            return _isPolling;
        }
    }

    private bool UpdateDetectedState(string state)
    {
        lock (_sync)
        {
            if (string.Equals(state, "NOMOVE", StringComparison.OrdinalIgnoreCase))
            {
                _noMoveSinceUtc ??= DateTimeOffset.UtcNow;
                var holdMs = (DateTimeOffset.UtcNow - _noMoveSinceUtc.Value).TotalMilliseconds;
                if (!_isDetected && holdMs >= NoMoveHoldMs)
                {
                    _isDetected = true;
                }

                return _isDetected;
            }

            _noMoveSinceUtc = null;
            if (string.Equals(state, "EMPTY", StringComparison.OrdinalIgnoreCase))
            {
                _isDetected = false;
            }

            return _isDetected;
        }
    }

    private static string NormalizePresence(string? rawResponse)
        => (rawResponse ?? string.Empty).Trim().ToUpperInvariant();
}
