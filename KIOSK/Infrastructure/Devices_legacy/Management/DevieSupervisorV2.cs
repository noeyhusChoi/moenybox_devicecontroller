using KIOSK.Device.Abstractions;
using KIOSK.Device.Core;
using KIOSK.Device.Transport;

namespace KIOSK.Devices.Management
{
    /// <summary>
    /// 장치 생명 주기 관리: 연결/해제, 상태 폴링, 명령 직렬화
    /// - 상태 저장/가공은 하지 않고, 이벤트만 발생시킨다.
    /// </summary>
    public sealed class DeviceSupervisorV2 : IAsyncDisposable
    {
        private readonly DeviceDescriptor _desc;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _pollingCts;

        private ITransport? _transport;
        private IDevice? _device;
        private DeviceProxy? _deviceProxy;

        public string Name => _desc.Name;
        public string Model => _desc.Model;

        public event Action<string>? Connected;
        public event Action<string>? Disconnected;
        public event Action<string, DeviceStatusSnapshot>? StatusUpdated;
        public event Action<string, Exception>? Faulted;

        public IDevice? Device => _deviceProxy ?? _device;

        internal T? GetInnerDevice<T>() where T : class, IDevice
        {
            if (_deviceProxy != null)
                return _deviceProxy.GetInner<T>();

            return _device as T;
        }

        public DeviceSupervisorV2(DeviceDescriptor desc)
        {
            _desc = desc ?? throw new ArgumentNullException(nameof(desc));
        }

        public async Task RunAsync(CancellationToken ct)
        {
            // 무한 재접속 루프 (취소될 때까지)
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 1. 트랜스포트/디바이스 생성
                    _transport = TransportFactory.Create(_desc);
                    _transport.Disconnected += (_, __) =>
                    {
                        // 장치 쪽에서 끊겼다고 알려줄 때
                        Disconnected?.Invoke(_desc.Name);
                        RequestReconnect();
                    };

                    var newDevice = DeviceRegistry.Create(_desc, _transport);
                    if (_deviceProxy is null)
                    {
                        _deviceProxy = new DeviceProxy(newDevice);
                    }
                    else
                    {
                        _deviceProxy.Swap(newDevice);
                    }

                    _device = newDevice;

                    // 2. 물리 연결
                    await _transport.OpenAsync(ct).ConfigureAwait(false);

                    // 3. 디바이스 초기화
                    var initSnapshot = await _device.InitializeAsync(ct).ConfigureAwait(false);
                    var hasError = HasError(initSnapshot);

                    if (initSnapshot != null)
                        StatusUpdated?.Invoke(_desc.Name, initSnapshot);

                    if (!hasError)
                    {
                        Connected?.Invoke(_desc.Name);

                        // 4. 상태 폴링 루프
                        using var linked = CreatePollingCts(ct);
                        var pollToken = linked.Token;
                        var pollMs = Math.Max(100, _desc.PollingMs);

                        while (!pollToken.IsCancellationRequested)
                        {
                            try
                            {
                                if (_device is null)
                                    throw new InvalidOperationException("Device not ready");

                                await _gate.WaitAsync(pollToken).ConfigureAwait(false);
                                try
                                {
                                    var sn = await _device.GetStatusAsync(pollToken, string.Empty).ConfigureAwait(false);
                                    if (sn == null)
                                        break;

                                    StatusUpdated?.Invoke(_desc.Name, sn);
                                }
                                finally
                                {
                                    _gate.Release();
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                RequestReconnect();
                                throw;
                            }

                            await Task.Delay(pollMs, pollToken).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // 초기화에서 에러 → 일정 시간 후 재시도
                        var reconnectDelayMs = Math.Max(100, _desc.PollingMs);
                        await Task.Delay(reconnectDelayMs, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
                {
                    // 외부 취소 → 종료
                    break;
                }
                catch (OperationCanceledException)
                {
                    // 내부 취소(재연결 요청) → 재시도 전 백오프
                    var reconnectDelayMs = Math.Max(100, _desc.PollingMs);
                    try
                    {
                        await Task.Delay(reconnectDelayMs, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                    continue;
                }
                catch (Exception ex)
                {
                    // 예기치 못한 오류 → Faulted + 재접속 대기
                    Faulted?.Invoke(_desc.Name, ex);
                    var reconnectDelayMs = Math.Max(100, _desc.PollingMs);
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
                    Interlocked.Exchange(ref _pollingCts, null)?.Dispose();
                    // 연결 종료/정리
                    await CleanupAsync(ct).ConfigureAwait(false);
                    // 여기서 Disconnected 이벤트를 한 번 더 보낼지 여부는 선택사항
                    // Disconnected?.Invoke(_desc.Name);
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
                Faulted?.Invoke(_desc.Name, ex);
                RequestReconnect();
                return new CommandResult(false, $"[{cmd.Name}] ERROR COMMAND: {ex.Message}");
            }
            finally
            {
                _gate.Release();
            }
        }

        private CancellationTokenSource CreatePollingCts(CancellationToken ct)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Interlocked.Exchange(ref _pollingCts, linked)?.Dispose();
            return linked;
        }

        private void RequestReconnect()
        {
            try { _pollingCts?.Cancel(); }
            catch { }
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
    }
}

