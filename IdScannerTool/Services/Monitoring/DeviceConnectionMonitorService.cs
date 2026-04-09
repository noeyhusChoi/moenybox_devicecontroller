using Microsoft.Extensions.Hosting;

namespace IdScannerTool.Services;

/// <summary>
/// 장치 연결 상태를 주기적으로 확인하고 끊김/복구 이벤트를 발행한다.
/// - 1초 폴링
/// - disconnect 연속 3회 시 fault
/// - fault는 래치 + 쿨다운 적용
/// </summary>
public sealed class DeviceConnectionMonitorService : BackgroundService, IDeviceConnectionMonitorService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan FaultCooldown = TimeSpan.FromSeconds(10);

    private readonly IDeviceManagerPort _runtimePort;
    private readonly string _deviceId;
    private int _disconnectStreak;
    private bool _faultLatched;
    private DateTimeOffset _lastFaultRaisedUtc = DateTimeOffset.MinValue;

    public DeviceConnectionMonitorService(IDeviceManagerPort runtimePort, string deviceId)
    {
        _runtimePort = runtimePort;
        _deviceId = deviceId;
    }

    public event EventHandler<DeviceConnectionFaultEvent>? ConnectionFaulted;
    public event EventHandler<DeviceConnectionRecoveredEvent>? ConnectionRecovered;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Monitor loop must stay alive.
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        var connection = await _runtimePort.GetConnectionAsync(_deviceId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var state = connection?.State ?? DeviceConnectionState.Disconnected;
        var message = ToConnectionMessage(state);
        var isDisconnected = state == DeviceConnectionState.Disconnected;

        if (isDisconnected)
        {
            _disconnectStreak++;

            if (_faultLatched || _disconnectStreak < 3)
            {
                return;
            }

            if (now - _lastFaultRaisedUtc < FaultCooldown)
            {
                return;
            }

            _faultLatched = true;
            _lastFaultRaisedUtc = now;
            ConnectionFaulted?.Invoke(this, new DeviceConnectionFaultEvent(
                DeviceId: _deviceId,
                ConnectionState: state,
                Message: message,
                ConsecutiveDisconnectCount: _disconnectStreak,
                TimestampUtc: now));
            return;
        }

        _disconnectStreak = 0;
        if (!_faultLatched)
        {
            return;
        }

        _faultLatched = false;
        ConnectionRecovered?.Invoke(this, new DeviceConnectionRecoveredEvent(
            DeviceId: _deviceId,
            ConnectionState: state,
            Message: message,
            TimestampUtc: now));
    }

    private static string ToConnectionMessage(DeviceConnectionState state)
        => state switch
        {
            DeviceConnectionState.Connected => "Connected",
            DeviceConnectionState.Connecting => "Connecting",
            DeviceConnectionState.Faulted => "Faulted",
            _ => "Disconnected"
        };
}
