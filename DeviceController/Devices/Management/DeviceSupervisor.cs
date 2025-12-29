using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using System.Collections.Generic;
using System.Diagnostics;

namespace KIOSK.Devices.Management
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
        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _attemptCts;
        private bool _connectFailEmitted;

        private ITransport? _transport;
        private IDevice? _device;

        public string Name => _desc.Name;
        public string Model => _desc.Model;

        public event Action<string>? Connected;
        public event Action<string>? Disconnected;
        public event Action<string, StatusSnapshot>? StatusUpdated;
        public event Action<string, Exception>? Faulted;

        public IDevice? Device => _device;

        internal T? GetInnerDevice<T>() where T : class, IDevice
            => _device as T;

        public DeviceSupervisor(DeviceDescriptor desc, ITransportFactory transportFactory, IDeviceFactory deviceFactory)
        {
            _desc = desc ?? throw new ArgumentNullException(nameof(desc));
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
            _deviceFactory = deviceFactory ?? throw new ArgumentNullException(nameof(deviceFactory));
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var reconnectDelayMs = Math.Max(100, _desc.PollingMs);
                var isPolling = false;

                try
                {
                    using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    var attemptToken = attemptCts.Token;
                    _attemptCts = attemptCts;

                    _transport = _transportFactory.Create(_desc);
                    _transport.Disconnected += (_, __) =>
                    {
                        SafeInvokeDisconnected();
                        try { attemptCts.Cancel(); } catch { }
                    };

                    _device = _deviceFactory.Create(_desc, _transport);

                    await _transport.OpenAsync(attemptToken).ConfigureAwait(false);

                    var initSnapshot = await _device.InitializeAsync(attemptToken).ConfigureAwait(false);
                    SafeInvokeStatusUpdated(initSnapshot);

                    if (!HasError(initSnapshot))
                    {
                        SafeInvokeConnected();
                        _connectFailEmitted = false;
                        isPolling = true;
                        await PollAsync(attemptToken).ConfigureAwait(false);
                    }

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
                    if (!isPolling && !_connectFailEmitted)
                    {
                        SafeInvokeStatusUpdated(CreateConnectFailSnapshot());
                        _connectFailEmitted = true;
                    }
                    else
                        SafeInvokeFaulted(ex);
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
                    isPolling = false;
                    _attemptCts = null;
                    await CleanupAsync(ct).ConfigureAwait(false);
                }
            }
        }

        public async Task<CommandResult> ExecuteAsync(DeviceCommand cmd, CancellationToken ct = default)
        {
            if (_device is null)
                return new CommandResult(false, string.Empty, Code: new ErrorCode("SYS", "APP", "STATE", "NOT_CONNECTED"));

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
                    catch { }

                    RequestReconnect();
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SafeInvokeFaulted(ex);
                RequestReconnect();
                return new CommandResult(false, string.Empty, Code: new ErrorCode("SYS", "APP", "INTERNAL", "COMMAND"));
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

        private async Task PollAsync(CancellationToken ct)
        {
            var pollMs = Math.Max(100, _desc.PollingMs);

            while (!ct.IsCancellationRequested)
            {
                if (_device is null)
                    throw new InvalidOperationException("Device not ready");

                await _gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var sn = await _device.GetStatusAsync(ct).ConfigureAwait(false);

                    SafeInvokeStatusUpdated(sn);
                }
                finally
                {
                    _gate.Release();
                }

                await Task.Delay(pollMs, ct).ConfigureAwait(false);
            }
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            try
            {
                if (_device is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (_device is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                _device = null;
            }

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
                await (_transport?.DisposeAsync() ?? ValueTask.CompletedTask);
            }
            catch
            {
            }
            finally
            {
                _transport = null;
            }
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

            if (snapshot.Alarms is null || snapshot.Alarms.Count == 0)
                return false;

            return snapshot.Alarms.Any(a => a.Severity is Severity.Error or Severity.Critical);
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

        private void SafeInvokeFaulted(Exception ex)
        {
            try { Faulted?.Invoke(_desc.Name, ex); }
            catch (Exception inner) { Trace.WriteLine(inner); }
        }

        private StatusSnapshot CreateConnectFailSnapshot()
        {
            var deviceKey = ResolveDeviceKey(_desc.Model);
            var code = new ErrorCode("DEV", deviceKey, "CONNECT", "FAIL");
            var alarm = new StatusEvent(
                code.ToString(),
                string.Empty,
                Severity.Error,
                DateTimeOffset.UtcNow,
                ErrorCode: code);

            return new StatusSnapshot
            {
                Name = _desc.Name,
                Model = _desc.Model,
                Health = DeviceHealth.Offline,
                Timestamp = DateTimeOffset.UtcNow,
                Alarms = new List<StatusEvent> { alarm }
            };
        }

        private static string ResolveDeviceKey(string model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return "DEVICE";

            return model.ToUpperInvariant() switch
            {
                "QR_TOTINFO" => "QR",
                "QR_NEWLAND" => "QR",
                "HCDM10K" => "HCDM",
                "HCDM20K" => "HCDM",
                "DEPOSIT" => "CASH",
                _ => model.ToUpperInvariant()
            };
        }
    }
}
