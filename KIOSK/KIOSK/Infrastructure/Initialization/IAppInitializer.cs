using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.Infrastructure.Initialization
{
    public interface IAppInitializer
    {
        /// <summary>
        /// 초기화 진행 텍스트가 업데이트될 때 발생한다.
        /// </summary>
        event Action<string>? ProgressChanged;

        /// <summary>
        /// 모든 초기화가 완료되면 true
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 초기화를 수행한다.
        /// </summary>
        Task InitializeAsync();
    }
}
