using KIOSK.Device.Abstractions;
using KIOSK.Infrastructure.Devices.Runtime.Factories;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIOSK.Infrastructure.Devices.Runtime
{
    /// <summary>
    /// 장치 생명 주기 관리: 연결/해제, 상태 폴링, 명령 직렬화
    /// - 상태 저장/가공은 하지 않고, 이벤트만 발생시킨다.
    /// </summary>
    public sealed class DeviceSupervisor : IAsyncDisposable
    {
        private readonly DeviceDescriptor _desc;
        private readonly ITransportFactory _transportFactory;
        private readonly IDeviceFactory _deviceFactory;
        private readonly ILogger<DeviceSupervisor> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _attemptCts;
        private bool _connectFailEmitted;
        private bool _connectedThisAttempt;
        private bool _isOnline;

        private ITransport? _transport;
        private IDevice? _device;

        public string DeviceId => _desc.EffectiveId;
        public string Name => _desc.Name;
        public string Model => _desc.Model;
        public string Vendor => _desc.Vendor;
        public string TransportType => _desc.TransportType;
        public string TransportPort => _desc.TransportPort;
        public string TransportParam => _desc.TransportParam;
        public string ProtocolName => _desc.ProtocolName;
        public int PollingMs => _desc.PollingMs;
        public string DeviceType => _desc.DeviceType;
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
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
            _deviceFactory = deviceFactory ?? throw new ArgumentNullException(nameof(deviceFactory));
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
                    await CleanupDeviceAsync().ConfigureAwait(false);
                    await CleanupTransportAsync(ct).ConfigureAwait(false);
                }
            }
        }

        public async Task<CommandResult> ExecuteAsync(DeviceCommand cmd, CancellationToken ct = default)
        {
            if (_device is null)
                return CreateNotConnectedCommandResult();

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var result = await _device.ExecuteAsync(cmd, ct).ConfigureAwait(false);
                if (result.Success && cmd.Name.Equals("RESTART", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (_transport is not null)
                            await _transport.CloseAsync(ct).ConfigureAwait(false);
                    }
                    catch
                    {
                    }

                    RequestReconnect();
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                RequestReconnect();
                return CreateTimeoutCommandResult();
            }
            catch
            {
                RequestReconnect();
                return CreateErrorCommandResult();
            }
            finally
            {
                _gate.Release();
            }
        }

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
            _logger.LogInformation("Session start. device={Device} type={DeviceType} driver={Driver} comm={CommType} port={CommPort} param={CommParam}",
                _desc.Name,
                _desc.DeviceType,
                _desc.Driver,
                _desc.TransportType,
                _desc.TransportPort,
                _desc.TransportParam);

            _transport = _transportFactory.Create(_desc);
            _transport.Disconnected += HandleTransportDisconnected;
            _device = _deviceFactory.Create(_desc, _transport);
            await _transport.OpenAsync(attemptToken).ConfigureAwait(false);
            var initSnapshot = await _device.InitializeAsync(attemptToken).ConfigureAwait(false);
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

            var pollMs = Math.Max(1000, _desc.PollingMs);
            while (!attemptToken.IsCancellationRequested)
            {
                await _gate.WaitAsync(attemptToken).ConfigureAwait(false);
                try
                {
                    var snapshot = await _device.GetStatusAsync(attemptToken).ConfigureAwait(false);
                    SafeInvokeStatusUpdated(snapshot);
                }
                finally
                {
                    _gate.Release();
                }

                await Task.Delay(pollMs, attemptToken).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _gate.Dispose();
            await CleanupDeviceAsync().ConfigureAwait(false);
            await CleanupTransportAsync(CancellationToken.None).ConfigureAwait(false);
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
            try { StatusUpdated?.Invoke(_desc.EffectiveId, snapshot); }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }

        private void SafeInvokeConnected()
        {
            try { Connected?.Invoke(_desc.EffectiveId); }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }

        private void SafeInvokeDisconnected()
        {
            try { Disconnected?.Invoke(_desc.EffectiveId); }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }

        private void HandleTransportDisconnected(object? sender, EventArgs e)
        {
            if (!_isOnline)
                return;

            _isOnline = false;
            _logger.LogWarning("Transport disconnected. device={Device} model={Model}", _desc.EffectiveId, _desc.Model);
            SafeInvokeStatusUpdated(CreateDisconnectedSnapshot());
            SafeInvokeDisconnected();
            try { _attemptCts?.Cancel(); } catch { }
        }

        private async Task CleanupDeviceAsync()
        {
            try
            {
                if (_device is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else if (_device is IDisposable disposable)
                    disposable.Dispose();
            }
            catch
            {
            }
            finally
            {
                _device = null;
            }
        }

        private async Task CleanupTransportAsync(CancellationToken ct)
        {
            try
            {
                if (_transport is not null)
                    await _transport.CloseAsync(ct).ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                if (_transport is not null)
                    await _transport.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                if (_transport is not null)
                    _transport.Disconnected -= HandleTransportDisconnected;
                _transport = null;
            }
        }

        private CommandResult CreateNotConnectedCommandResult()
        {
            var deviceKey = string.IsNullOrWhiteSpace(_desc.DeviceType) ? _desc.Model : _desc.DeviceType;
            var code = new ErrorCode("DEV", deviceKey, "COMMAND", "NOT_CONNECTED");
            return new CommandResult(false, string.Empty, Code: code);
        }

        private CommandResult CreateTimeoutCommandResult()
        {
            var deviceKey = string.IsNullOrWhiteSpace(_desc.DeviceType) ? _desc.Model : _desc.DeviceType;
            var code = new ErrorCode("DEV", deviceKey, "COMMAND", "TIMEOUT");
            return new CommandResult(false, string.Empty, Code: code, Retryable: true);
        }

        private CommandResult CreateErrorCommandResult()
        {
            var deviceKey = string.IsNullOrWhiteSpace(_desc.DeviceType) ? _desc.Model : _desc.DeviceType;
            var code = new ErrorCode("DEV", deviceKey, "COMMAND", "ERROR");
            return new CommandResult(false, string.Empty, Code: code);
        }

        private void LogRunException(Exception ex, bool connected)
        {
            if (connected)
            {
                _logger.LogError(ex, "Supervisor run failed. device={Device} model={Model}", _desc.EffectiveId, _desc.Model);
                return;
            }

            if (!_connectFailEmitted)
                _logger.LogError(ex, "Supervisor connect failed. device={Device} model={Model}", _desc.EffectiveId, _desc.Model);
        }

        private StatusSnapshot CreateConnectFailSnapshot()
        {
            var code = new ErrorCode("DEV", _desc.DeviceType, "CONNECT", "FAIL");
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
