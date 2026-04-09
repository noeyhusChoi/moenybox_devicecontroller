using DeviceKit.Composition;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DeviceKit.Engine;

/// <summary>
/// Shared single-loop polling runtime engine.
/// - One scheduler loop manages status polling for all registered devices.
/// - Per-device gates serialize connect/disconnect/execute/status operations.
/// </summary>
public sealed class DeviceRuntimePort : IDeviceManagerPort, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, DeviceSlot> _slots;
    private readonly DeviceRuntimeOptions _options;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly CancellationTokenSource _schedulerCts = new();
    private readonly Task _schedulerTask;
    private readonly int _schedulerTickMs;

    #region Events
    public event Action<StatusSnapshot>? DeviceStatusObserved;
    public event Action<DeviceConnectionSnapshot>? ConnectionObserved;
    public event Action<DeviceEventEnvelope>? DeviceEventReceived;

    #endregion

    #region Construction

    public DeviceRuntimePort(
        IEnumerable<DeviceDescriptor> descriptors,
        DeviceRuntimeOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        _options = (options ?? DeviceRuntimeOptions.Default).Normalize();
        _loggerFactory = loggerFactory;

        // 장치 유효성 검사 및 등록
        _slots = new ConcurrentDictionary<string, DeviceSlot>(StringComparer.OrdinalIgnoreCase);
        foreach (var descriptor in descriptors.Where(d => d.Validate))
        {
            _slots.TryAdd(descriptor.EffectiveId, new DeviceSlot(descriptor));
        }

        // 스케줄러 시작 (등록된 장치들의 폴링 주기를 기반으로 적절한 틱 계산)
        _schedulerTickMs = _options.SchedulerTickMs ?? CalculateSchedulerTickMs(_slots.Values);
        _schedulerTask = Task.Run(() => RunSchedulerAsync(_schedulerCts.Token));
    }

    #endregion

    #region Registration And Queries

    /// <summary>
    /// 장치를 런타임에 등록하고, 즉시 자동 연결/폴링 대상에 포함시킵니다.
    /// </summary>
    public Task AddAsync(DeviceDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 유효성 검사
        if (descriptor is null)
            throw new ArgumentNullException(nameof(descriptor));

        if (!descriptor.Validate)
            throw new ArgumentException("Descriptor validation is disabled.", nameof(descriptor));

        if (string.IsNullOrWhiteSpace(descriptor.EffectiveId))
            throw new ArgumentException("Device id is required.", nameof(descriptor));


        // 장치 등록
        var slot = new DeviceSlot(descriptor);
        if (!_slots.TryAdd(descriptor.EffectiveId, slot))
            throw new InvalidOperationException($"Duplicated device id: {descriptor.EffectiveId}");

        // 초기 상태 업데이트
        ObserveConnectionSnapshot(slot);
        ObserveStatusSnapshot(slot);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 장치 ID로 등록 정보를 조회합니다.
    /// </summary>
    public bool TryGetDevice(string deviceId, out DeviceDescriptor info)
    {
        if (_slots.TryGetValue(deviceId, out var slot))
        {
            info = slot.Descriptor;
            return true;
        }

        info = default!;
        return false;
    }

    /// <summary>
    /// 현재 등록된 전체 장치 정보를 반환합니다.
    /// </summary>
    public IReadOnlyList<DeviceDescriptor> GetAllDevices()
        => _slots.Values
            .Select(x => x.Descriptor)
            .ToList();

    /// <summary>
    /// 전체 장치의 장치 상태 목록을 반환합니다.
    /// </summary>
    public Task<IReadOnlyList<StatusSnapshot>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = _slots.Values
            .Select(x => x.StatusSnapshot)
            .ToList();

        return Task.FromResult((IReadOnlyList<StatusSnapshot>)snapshots);
    }

    /// <summary>
    /// 장치 ID에 해당하는 장치 상태를 반환합니다.
    /// </summary>
    public Task<StatusSnapshot?> GetStatusAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return Task.FromResult<StatusSnapshot?>(null);

        return Task.FromResult<StatusSnapshot?>(slot.StatusSnapshot);
    }

    /// <summary>
    /// 전체 장치의 연결 상태 목록을 반환합니다.
    /// </summary>
    public Task<IReadOnlyList<DeviceConnectionSnapshot>> GetConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = _slots.Values
            .Select(x => x.ConnectionSnapshot)
            .ToList();

        return Task.FromResult((IReadOnlyList<DeviceConnectionSnapshot>)snapshots);
    }

    /// <summary>
    /// 장치 ID에 해당하는 연결 상태를 반환합니다.
    /// </summary>
    public Task<DeviceConnectionSnapshot?> GetConnectionAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return Task.FromResult<DeviceConnectionSnapshot?>(null);

        return Task.FromResult<DeviceConnectionSnapshot?>(slot.ConnectionSnapshot);
    }

    /// <summary>
    /// 장치가 지원하는 명령 이름 목록을 반환합니다.
    /// 연결된 경우에는 실제 handle 기준, 미연결인 경우에는 registry 정의를 기준으로 조회합니다.
    /// </summary>
    public IReadOnlyCollection<string> GetCommands(string deviceId)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return Array.Empty<string>();

        if (slot.Handle is not null)
            return slot.Handle.Commands.Select(x => x.Name).ToArray();

        return DeviceDriverRegistry
            .GetSupportedCommands(slot.Descriptor.DriverType)
            .Select(x => x.Name)
            .ToArray();
    }

    #endregion

    #region Connection And Commands

    /// <summary>
    /// 장치 연결을 즉시 시도합니다.
    /// 이미 연결된 슬롯이면 false를 반환합니다.
    /// </summary>
    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return false;

        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ConnectSlotAsync(slot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    /// <summary>
    /// 장치 연결 해제하고 자동 재연결 대상에서도 제외합니다.
    /// </summary>
    public async Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return false;

        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await DisconnectSlotAsync(slot, cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    /// <summary>
    /// 장치 명령을 직렬화된 gate 안에서 실행합니다.
    /// 실행 성공 후에는 즉시 상태 재조회가 일어나도록 다음 스케줄을 당깁니다.
    /// </summary>
    public async Task<DeviceCommandResponse> ExecuteAsync(string deviceId, DeviceCommandRequest command, CancellationToken cancellationToken = default)
    {
        if (!_slots.TryGetValue(deviceId, out var slot))
            return new DeviceCommandResponse(false, $"Device not found. deviceId={deviceId}", Code: new ErrorCode("DEV", "RUNTIME", "COMMAND", "DEVICE_ID_NOT_FOUND"));

        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (slot.Handle?.Driver is null)
            {
                var target = string.IsNullOrWhiteSpace(slot.Descriptor.DeviceType)
                    ? "UNKNOWN_TARGET"
                    : slot.Descriptor.DeviceType.Trim().ToUpperInvariant();

                return new DeviceCommandResponse(false, string.Empty, Code: new ErrorCode("DEV", target, "COMMAND", "NOT_CONNECTED"));
            }

            try
            {
                var result = await slot.Handle.Driver.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
                slot.NextPollAt = DateTimeOffset.UtcNow;
                return result;
            }
            catch (Exception ex)
            {
                await FailSlotAsync(slot, ex, cancellationToken).ConfigureAwait(false);
                return new DeviceCommandResponse(false, ex.Message, Code: new ErrorCode("DEV", "RUNTIME", "COMMAND", "EXECUTION_ERROR"));
            }
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    #endregion

    #region Lifetime

    /// <summary>
    /// 스케줄러를 중지하고 모든 장치 세션과 gate를 정리합니다.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _schedulerCts.Cancel();
        try
        {
            await _schedulerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _schedulerCts.Dispose();
        }

        foreach (var slot in _slots.Values)
        {
            await slot.Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisposeSessionAsync(slot, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                slot.Gate.Release();
                slot.Gate.Dispose();
            }
        }
    }

    #endregion

    #region Scheduler

    private async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var dueSlots = _slots.Values
                .Where(x => x.NextPollAt <= now)
                .OrderBy(x => x.NextPollAt)
                .ToArray();

            foreach (var slot in dueSlots)
            {
                await RunScheduledCycleAsync(slot, cancellationToken).ConfigureAwait(false);
            }

            await Task.Delay(_schedulerTickMs, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 스케줄 대상 슬롯을 한 번 처리합니다.
    /// 연결이 없으면 연결을 시도하고, 연결이 있으면 상태를 갱신합니다.
    /// </summary>
    private async Task RunScheduledCycleAsync(DeviceSlot slot, CancellationToken cancellationToken)
    {
        await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (slot.Handle?.Driver is null)
            {
                await ConnectSlotAsync(slot, cancellationToken).ConfigureAwait(false);
                return;
            }

            await RefreshStatusAsync(slot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    /// <summary>
    /// 슬롯에 드라이버 세션이 없을 때 새로 연결을 수립합니다.
    /// </summary>
    private async Task<bool> ConnectSlotAsync(DeviceSlot slot, CancellationToken cancellationToken)
    { if (slot.Handle is not null)
            return false;

        DeviceDriverHandle? handle = null;

        try
        {
            handle = DeviceDriverRegistry.CreateHandle(slot.Descriptor, _loggerFactory);
            var init = await handle.Driver.InitializeAsync(cancellationToken).ConfigureAwait(false);

            var observedAt = DateTimeOffset.UtcNow;
            ActivateSession(slot, handle);
            slot.FailCount = 0;
            slot.NextPollAt = observedAt;
            slot.StatusSnapshot = init with
            {
                DeviceId = slot.Descriptor.EffectiveId,
                DeviceType = slot.Descriptor.DeviceType,
                Name = slot.Descriptor.Name,
                Model = slot.Descriptor.Model
            };
            ObserveStatusSnapshot(slot);
            slot.ConnectionSnapshot = slot.ConnectionSnapshot with
            {
                State = DeviceConnectionState.Connected,
                Timestamp = observedAt
            };
            ObserveConnectionSnapshot(slot);

            PublishEvent(slot.Descriptor, DeviceEventNames.Connected, new { state = "connected" });
            return true;
        }
        catch (Exception ex)
        {
            if (slot.Handle is null && handle is not null)
            {
                try
                {
                    await handle.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                }
            }

            await FailSlotAsync(slot, ex, cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    /// <summary>
    /// 연결된 슬롯의 장치 상태를 조회하고 다음 폴링 시점을 갱신합니다.
    /// 연결 상태는 여기서 변경하지 않고, 연결 생명주기 메서드에서만 갱신합니다.
    /// </summary>
    private async Task RefreshStatusAsync(DeviceSlot slot, CancellationToken cancellationToken)
    {
        try
        {
            var status = await slot.Handle!.Driver.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            slot.FailCount = 0;
            slot.NextPollAt = DateTimeOffset.UtcNow.AddMilliseconds(GetPollingMs(slot));
            slot.StatusSnapshot = status with
            {
                DeviceId = slot.Descriptor.EffectiveId,
                DeviceType = slot.Descriptor.DeviceType,
                Name = slot.Descriptor.Name,
                Model = slot.Descriptor.Model
            };
            ObserveStatusSnapshot(slot);
        }
        catch (Exception ex)
        {
            await FailSlotAsync(slot, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Snapshot Observation

    /// <summary>
    /// 상태 스냅샷을 최신 관측값으로 교체하고 이벤트를 발행합니다.
    /// </summary>
    private void ObserveStatusSnapshot(DeviceSlot slot)
    {
        try
        {
            DeviceStatusObserved?.Invoke(slot.StatusSnapshot);
        }
        catch
        {
        }
    }

    /// <summary>
    /// 연결 스냅샷을 최신 관측값으로 교체하고 이벤트를 발행합니다.
    /// </summary>
    private void ObserveConnectionSnapshot(DeviceSlot slot)
    {
        try
        {
            ConnectionObserved?.Invoke(slot.ConnectionSnapshot);
        }
        catch
        {
        }
    }

    /// <summary>
    /// 등록된 장치들의 최소 polling 주기를 기준으로 스케줄러 주기를 계산합니다.
    /// </summary>
    private int CalculateSchedulerTickMs(IEnumerable<DeviceSlot> slots)
    {
        var minPolling = slots
            .Select(GetPollingMs)
            .DefaultIfEmpty(_options.DefaultPollingMs)
            .Min();

        return minPolling / 2;
    }

    #endregion

    #region Runtime Flow

    /// <summary>
    /// 슬롯에 등록된 descriptor 기준 polling 주기를 런타임 최소값과 기본값 정책에 맞춰 정규화합니다.
    /// </summary>
    private int GetPollingMs(DeviceSlot slot)
        => Math.Max(_options.MinPollingMs, slot.Descriptor.PollingMs > 0 ? slot.Descriptor.PollingMs : _options.DefaultPollingMs);

    /// <summary>
    /// 연속 실패 횟수에 따라 다음 재연결까지의 backoff 시간을 계산합니다.
    /// </summary>
    private int GetBackoffMs(DeviceSlot slot)
    {
        var baseMs = GetPollingMs(slot);
        var factor = 1 << Math.Clamp(slot.FailCount - 1, 0, 4);
        return Math.Min(baseMs * factor, _options.MaxBackoffMs);
    }

    #endregion

    /// <summary>
    /// 슬롯을 명시적으로 연결 해제 상태로 전환합니다.
    /// </summary>
    private async Task DisconnectSlotAsync(DeviceSlot slot, CancellationToken cancellationToken)
    {
        try
        {
            await DisposeSessionAsync(slot, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }

        var observedAt = DateTimeOffset.UtcNow;
        slot.FailCount = 0;
        slot.NextPollAt = DateTimeOffset.MaxValue;
        slot.StatusSnapshot = slot.StatusSnapshot with
        {
            Alerts = new List<StatusEvent>(),
            Timestamp = observedAt
        };
        ObserveStatusSnapshot(slot);

        slot.ConnectionSnapshot = slot.ConnectionSnapshot with
        {
            State = DeviceConnectionState.Disconnected,
            Timestamp = observedAt
        };
        ObserveConnectionSnapshot(slot);

        PublishEvent(slot.Descriptor, DeviceEventNames.Disconnected, new { state = "disconnected" });
    }

    /// <summary>
    /// 슬롯에 연결된 드라이버 세션과 이벤트 구독을 정리합니다.
    /// </summary>
    private async Task DisposeSessionAsync(DeviceSlot slot, CancellationToken cancellationToken)
    {
        var handle = slot.Handle;

        DetachDriverEvents(slot);
        slot.Handle = null;

        if (handle is not null)
            await handle.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 초기화가 끝난 드라이버 세션을 슬롯에 활성화합니다.
    /// </summary>
    private void ActivateSession(DeviceSlot slot, DeviceDriverHandle handle)
    {
        slot.Handle = handle;
        AttachDriverEvents(slot);
    }

    /// <summary>
    /// 슬롯 실패를 공통 방식으로 처리하고 다음 재연결 시점을 예약합니다.
    /// </summary>
    private async Task FailSlotAsync(DeviceSlot slot, Exception ex, CancellationToken cancellationToken)
    {
        var observedAt = DateTimeOffset.UtcNow;

        slot.FailCount++;

        try
        {
            await DisposeSessionAsync(slot, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }

        slot.NextPollAt = DateTimeOffset.UtcNow.AddMilliseconds(GetBackoffMs(slot));
        slot.StatusSnapshot = slot.StatusSnapshot with
        {
            Alerts = new List<StatusEvent>(),
            Timestamp = observedAt
        };
        ObserveStatusSnapshot(slot);
        slot.ConnectionSnapshot = slot.ConnectionSnapshot with
        {
            State = DeviceConnectionState.Disconnected,
            Timestamp = observedAt
        };
        ObserveConnectionSnapshot(slot);
    }

    #region Driver Event Bridge

    /// <summary>
    /// 드라이버가 발생시키는 장치 이벤트를 런타임 이벤트로 브리지합니다.
    /// </summary>
    private void AttachDriverEvents(DeviceSlot slot)
    {
        DetachDriverEvents(slot);

        if (slot.Handle?.Driver is not IDeviceEventSource eventSource)
            return;

        EventHandler<DeviceDriverEvent> handler = (_, eventData) =>
            PublishEvent(
                slot.Descriptor,
                eventData.EventName,
                eventData.Payload,
                eventData.Version);

        eventSource.EventOccurred += handler;
        slot.EventSource = eventSource;
        slot.EventHandler = handler;
    }

    /// <summary>
    /// 슬롯에 연결된 드라이버 이벤트 구독을 안전하게 해제합니다.
    /// </summary>
    private void DetachDriverEvents(DeviceSlot slot)
    {
        try
        {
            if (slot.EventSource is not null && slot.EventHandler is not null)
                slot.EventSource.EventOccurred -= slot.EventHandler;
        }
        catch { }

        slot.EventSource = null;
        slot.EventHandler = null;
    }

    /// <summary>
    /// 드라이버 이벤트를 공용 envelope 형태로 변환해 외부 구독자에게 전달합니다.
    /// </summary>
    private void PublishEvent(DeviceDescriptor descriptor, string eventName, object? payload, int version = 1)
    {
        if (DeviceEventReceived is null)
            return;

        var envelope = new DeviceEventEnvelope(
            descriptor.EffectiveId,
            descriptor.DeviceType ?? string.Empty,
            eventName,
            DateTimeOffset.UtcNow,
            DeviceEventJson.Serialize(payload),
            version);

        try
        {
            DeviceEventReceived.Invoke(envelope);
        }
        catch
        {
        }
    }

    #endregion

    #region Slot

    /// <summary>
    /// 런타임이 장치 1대를 추적하는 내부 상태 컨테이너입니다.
    /// </summary>
    private sealed class DeviceSlot
    {
        public DeviceSlot(DeviceDescriptor descriptor)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            StatusSnapshot = new StatusSnapshot
            {
                DeviceId = descriptor.EffectiveId,
                DeviceType = descriptor.DeviceType,
                Name = descriptor.Name,
                Model = descriptor.Model,
                Timestamp = DateTimeOffset.UtcNow
            };
            ConnectionSnapshot = new DeviceConnectionSnapshot
            {
                DeviceId = descriptor.EffectiveId,
                DeviceType = descriptor.DeviceType,
                Name = descriptor.Name,
                Model = descriptor.Model,
                State = DeviceConnectionState.Disconnected,
                Timestamp = DateTimeOffset.UtcNow
            };
            NextPollAt = DateTimeOffset.UtcNow;
        }

        public DeviceDescriptor Descriptor { get; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public DeviceDriverHandle? Handle { get; set; }
        public StatusSnapshot StatusSnapshot { get; set; }
        public DeviceConnectionSnapshot ConnectionSnapshot { get; set; }
        public DateTimeOffset NextPollAt { get; set; }
        public int FailCount { get; set; }

        public IDeviceEventSource? EventSource { get; set; }
        public EventHandler<DeviceDriverEvent>? EventHandler { get; set; }
    }

    #endregion
}
