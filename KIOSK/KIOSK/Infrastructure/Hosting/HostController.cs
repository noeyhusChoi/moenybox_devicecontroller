using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Hosting
{
    /// <summary>
    /// Generic Host의 수명(Start/Stop)을 안전하게 제어하는 컨트롤러
    /// - StartAsync / StopAsync 호출 중 예외가 발생해도 프로그램이 종료되지 않도록 보호
    /// - 여러 번 호출해도 중복 실행 방지
    /// - Host의 상태(Started, Stopping, Faulted)를 외부로 안전하게 제공
    /// </summary>
    public sealed class HostController : IHostController
    {
        private readonly IHost _host;
        private readonly ILogger<HostController>? _logger;

        private int _started;
        private int _stopping;
        private bool _disposed;

        public bool IsStarted => Volatile.Read(ref _started) == 1;
        public bool IsStopping => Volatile.Read(ref _stopping) == 1;

        /// <summary>Host 시작 중 예외가 발생했는지 여부</summary>
        public bool StartFaulted { get; private set; }

        /// <summary>Host 정지 중 예외가 발생했는지 여부</summary>
        public bool StopFaulted { get; private set; }

        //public IHost Host => _host;

        public HostController(IHost host, ILogger<HostController>? logger = null)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
            {
                _logger?.LogDebug("HostController.StartAsync(): 이미 시작됨");
                return;
            }

            try
            {
                _logger?.LogInformation("Host 시작 중...");
                await _host.StartAsync(cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation("Host 시작 완료");
            }
            catch (Exception ex)
            {
                StartFaulted = true;
                Interlocked.Exchange(ref _started, 0); // 실패 시 다시 초기화
                _logger?.LogError(ex, "Host 시작 중 예외 발생");
               
                // throw; 앱 종료 방지
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                _logger?.LogDebug("HostController.StopAsync(): 이미 정지 중");
                return;
            }

            try
            {
                _logger?.LogInformation("Host 정지 중...");
                await _host.StopAsync(cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation("Host 정지 완료");
            }
            catch (Exception ex)
            {
                StopFaulted = true;
                _logger?.LogError(ex, "Host 정지 중 예외 발생");

                // throw; 앱 종료 방지
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                if (IsStarted && !IsStopping)
                    await StopAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "HostController Dispose 중 예외 발생");
            }
            finally
            {
                _host.Dispose();
            }
        }
    }
}
