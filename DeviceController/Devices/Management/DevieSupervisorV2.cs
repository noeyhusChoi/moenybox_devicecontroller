using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Device.Transport;
using System.Diagnostics;

namespace KIOSK.Devices.Management
{
    /// <summary>
    /// 장치 생명 주기 관리: 연결/해제, 상태 폴링, 명령 직렬화
    /// - 상태 저장/가공은 하지 않고, 이벤트만 발생시킨다.
    /// </summary>
    public sealed class DeviceSupervisorV2 : IAsyncDisposable
    {
        private readonly DeviceDescriptor _desc;
        private readonly ITransportFactory _transportFactory;
        private readonly IDeviceFactory _deviceFactory;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _attemptCts;

        private ITransport? _transport;
        private IDevice? _device;

        public string Name => _desc.Name;
        public string Model => _desc.Model;

        public event Action<string>? Connected;
        public event Action<string>? Disconnected;
        public event Action<string, DeviceStatusSnapshot>? StatusUpdated;
        public event Action<string, Exception>? Faulted;

        public IDevice? Device => _device;

        internal T? GetInnerDevice<T>() where T : class, IDevice
            => _device as T;

        public DeviceSupervisorV2(DeviceDescriptor desc, ITransportFactory transportFactory, IDeviceFactory deviceFactory)
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
                    if (initSnapshot != null)
                        SafeInvokeStatusUpdated(initSnapshot);

                    if (!HasError(initSnapshot))
                    {
                        SafeInvokeConnected();
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
                    _attemptCts = null;
                    await CleanupAsync(ct).ConfigureAwait(false);
                }
            }
        }

        public async Task<CommandResult> ExecuteAsync(DeviceCommand cmd, CancellationToken ct = default)
        {
            if (_device is null)
                return new CommandResult(false, "Device not connected");

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await _device.ExecuteAsync(cmd, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SafeInvokeFaulted(ex);
                RequestReconnect();
                return new CommandResult(false, $"[{cmd.Name}] ERROR COMMAND: {ex.Message}");
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
                    if (sn == null)
                        break;

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

        private static bool HasError(DeviceStatusSnapshot? snapshot)
        {
            if (snapshot is null)
                return false;

            if (snapshot.Alarms is null || snapshot.Alarms.Count == 0)
                return false;

            return snapshot.Alarms.Any(a => a.Severity is Severity.Error or Severity.Critical);
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

        private void SafeInvokeStatusUpdated(DeviceStatusSnapshot snapshot)
        {
            try { StatusUpdated?.Invoke(_desc.Name, snapshot); }
            catch (Exception ex) { Trace.WriteLine(ex); }
        }

        private void SafeInvokeFaulted(Exception ex)
        {
            try { Faulted?.Invoke(_desc.Name, ex); }
            catch (Exception inner) { Trace.WriteLine(inner); }
        }
    }
}
