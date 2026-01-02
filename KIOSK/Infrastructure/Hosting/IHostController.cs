using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Hosting
{
    /// <summary>
    /// Generic Host(IHost)의 수명(Start/Stop)을 제어하는 컨트롤러
    /// </summary>
    public interface IHostController : IAsyncDisposable
    {
        /// <summary>Host를 시작 (여러 번 호출해도 한 번만 실제 시작)</summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>Host를 정지 (여러 번 호출해도 한 번만 실제 정지)</summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>Host가 이미 StartAsync를 정상적으로 통과했는지 여부</summary>
        bool IsStarted { get; }

        /// <summary>StopAsync 요청이 들어간 상태인지 여부</summary>
        bool IsStopping { get; }

        ///// <summary>내부 IHost를 노출해야 하면(디버깅용) 옵션으로 제공</summary>
        //IHost Host { get; }
    }
}
