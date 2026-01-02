using KIOSK.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.UI.Navigation.State
{
    public sealed class NavigationState
    {
        public IRootShellHost? RootShell { get; set; }

        // TopShell: RootShellViewModel, EnvironmentShellViewModel 등
        public ITopShellHost? ActiveTopShell { get; set; }

        // SubShell: MenuSubShell, ExchangeSubShell, GtfSubShell 등
        public ISubShellHost? ActiveSubShell { get; set; }

        // FlowView: SubShell 내부 화면
        public object? ActiveFlowView { get; set; }

        // DI 스코프들 (서브쉘 / 플로우)
        public IServiceScope? SubShellScope { get; set; }
        public IServiceScope? FlowScope { get; set; }

        // 취소 토큰 (Flow 화면)
        public CancellationTokenSource? FlowCancellation { get; set; }

        // 모든 상태 초기화
        public void ResetAll()
        {
            FlowCancellation?.Cancel();
            FlowCancellation?.Dispose();
            FlowCancellation = null;

            FlowScope?.Dispose();
            FlowScope = null;

            SubShellScope?.Dispose();
            SubShellScope = null;

            ActiveFlowView = null;
            ActiveSubShell = null;
            ActiveTopShell = null;
        }
    }
}
