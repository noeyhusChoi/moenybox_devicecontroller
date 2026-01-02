using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Devices.Management
{
    /// <summary>
    /// 장치 생명 주기 관리: 연결/해제, 상태 폴링, 명령 직렬화
    /// - 상태 저장/가공은 하지 않고, 이벤트만 발생시킨다.
    /// </summary>
    public sealed class DeviceSupervisor : IAsyncDisposable
    {
        private readonly DeviceDescriptor _desc;
        private readonly SupervisorSession _session;
        private readonly SupervisorStatusPoller _poller;
        private readonly SupervisorCommandExecutor _executor;
        private readonly ILogger<DeviceSupervisor> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _attemptCts;
        private bool _connectFailEmitted;
        private bool _connectedThisAttempt;
        private bool _isOnline;

        private ITransport? _transport;
        private IDevice? _device;

        public string Name => _desc.Name;
        public string Model => _desc.Model;
        public string DriverKey => DeviceDriverResolver.Resolve(_desc);
        public string Vendor => _desc.Vendor;
        public string TransportType => _desc.TransportType;
        public string TransportPort => _desc.TransportPort;
        public string TransportParam => _desc.TransportParam;
        public string ProtocolName => _desc.ProtocolName;
        public int PollingMs => _desc.PollingMs;
        public string DeviceKey => _desc.DeviceKey;
        public string Driver => _desc.Driver;

        public event Action<string>? Connected;
        public event Action<string>? Disconnected;
        public event Action<string, StatusSnapshot>? StatusUpdated;

        public IDevice? Device => _device;

        internal T? GetInnerDevice<T>() where T : class, IDevice
            => _device as T;

        public DeviceSupervisor(
            DeviceDescriptor desc,
            ITransportFactory transportFactory,
            IDeviceFactory deviceFactory,
            ILogger<DeviceSupervisor>? logger = null)
        {
            _desc = desc ?? throw new ArgumentNullException(nameof(desc));
            _session = new SupervisorSession(desc, transportFactory, deviceFactory);
            _poller = new SupervisorStatusPoller(desc);
            _executor = new SupervisorCommandExecutor(desc);
            _session.TransportDisconnected += OnTransportDisconnected;
            _logger = logger ?? NullLogger<DeviceSupervisor>.Instance;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var reconnectDelayMs = Math.Max(100, _desc.PollingMs);
                _connectedThisAttempt = false;

                try
                {
                    using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    var attemptToken = attemptCts.Token;
                    _attemptCts = attemptCts;

                    await RunSessionAsync(attemptToken).ConfigureAwait(false);

                    await Task.Delay(reconnectDelayMs, attemptToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        await Task.Delay(reconnectDelayMs, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                }
                catch (Exception ex)
                {
                    LogRunException(ex, _connectedThisAttempt);
                    HandleRunException(_connectedThisAttempt);

                    try
                    {
                        await Task.Delay(reconnectDelayMs, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                finally
                {
                    _connectedThisAttempt = false;
                    _attemptCts = null;
                    _isOnline = false;
                    await _session.StopAsync(ct).ConfigureAwait(false);
                    _device = null;
                    _transport = null;
                }
            }
        }

        public async Task<CommandResult> ExecuteAsync(DeviceCommand cmd, CancellationToken ct = default)
            => await _executor.ExecuteAsync(_device, _transport, _gate, cmd, ct, RequestReconnect).ConfigureAwait(false);

        private void RequestReconnect()
        {
            try { _attemptCts?.Cancel(); }
            catch { }
        }

        private void HandleRunException(bool connected)
        {
            if (!connected && !_connectFailEmitted)
            {
                SafeInvokeStatusUpdated(CreateConnectFailSnapshot());
                _connectFailEmitted = true;
                SafeInvokeDisconnected();
            }
            else
            {
                SafeInvokeStatusUpdated(CreateDisconnectedSnapshot());
                SafeInvokeDisconnected();
            }

            _isOnline = false;
        }
        private async Task RunSessionAsync(CancellationToken attemptToken)
        {
            var initSnapshot = await _session.StartAsync(attemptToken).ConfigureAwait(false);
            _transport = _session.Transport;
            _device = _session.Device;
            SafeInvokeStatusUpdated(initSnapshot);

            if (HasError(initSnapshot))
                return;

            _connectedThisAttempt = true;
            _isOnline = true;
            SafeInvokeStatusUpdated(CreateConnectedSnapshot());
            SafeInvokeConnected();
            _connectFailEmitted = false;

            if (_device is null)
                return;

            await _poller.RunAsync(_device, _gate, attemptToken, SafeInvokeStatusUpdated).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync()
        {
            _gate.Dispose();
            return ValueTask.CompletedTask;
        }

        private static bool HasError(StatusSnapshot? snapshot)
        {
            if (snapshot is null)
                return false;

            if (snapshot.Alerts is null || snapshot.Alerts.Count == 0)
                return false;

            return snapshot.Alerts.Any(a => a.Severity is Severity.Error or Severity.Critical);
        }

        private void SafeInvokeStatusUpdated(StatusSnapshot snapshot)
        {
            try { StatusUpdated?.Invoke(_desc.Name, snapshot); }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }

        private void SafeInvokeConnected()
        {
            try { Connected?.Invoke(_desc.Name); }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }

        private void SafeInvokeDisconnected()
        {
            try { Disconnected?.Invoke(_desc.Name); }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }

        private void OnTransportDisconnected()
        {
            if (!_isOnline)
                return;

            _isOnline = false;
            _logger.LogWarning("Transport disconnected. device={Device} model={Model}", _desc.Name, _desc.Model);
            SafeInvokeStatusUpdated(CreateDisconnectedSnapshot());
            SafeInvokeDisconnected();
            try { _attemptCts?.Cancel(); } catch { }
        }

        private void LogRunException(Exception ex, bool connected)
        {
            if (connected)
            {
                _logger.LogError(ex, "Supervisor run failed. device={Device} model={Model}", _desc.Name, _desc.Model);
                return;
            }

            if (!_connectFailEmitted)
                _logger.LogError(ex, "Supervisor connect failed. device={Device} model={Model}", _desc.Name, _desc.Model);
        }

        private StatusSnapshot CreateConnectFailSnapshot()
        {
            var deviceKey = string.IsNullOrWhiteSpace(_desc.DeviceKey)
                ? _desc.Model
                : _desc.DeviceKey;
            var code = new ErrorCode("DEV", deviceKey, "CONNECT", "FAIL");
            var alert = new StatusEvent(
                code.ToString(),
                string.Empty,
                Severity.Error,
                DateTimeOffset.UtcNow,
                ErrorCode: code,
                Source: AlertSource.Connection);

            return new StatusSnapshot
            {
                Name = _desc.Name,
                Model = _desc.Model,
                Health = DeviceHealth.Offline,
                Timestamp = DateTimeOffset.UtcNow,
                Alerts = new List<StatusEvent> { alert },
                AlertScope = AlertSource.Connection
            };
        }

        private StatusSnapshot CreateConnectedSnapshot()
            => new StatusSnapshot
            {
                Name = _desc.Name,
                Model = _desc.Model,
                Health = DeviceHealth.Online,
                Timestamp = DateTimeOffset.UtcNow,
                AlertScope = AlertSource.Connection
            };

        private StatusSnapshot CreateDisconnectedSnapshot()
            => new StatusSnapshot
            {
                Name = _desc.Name,
                Model = _desc.Model,
                Health = DeviceHealth.Offline,
                Timestamp = DateTimeOffset.UtcNow,
                AlertScope = AlertSource.Connection
            };
    }
}
