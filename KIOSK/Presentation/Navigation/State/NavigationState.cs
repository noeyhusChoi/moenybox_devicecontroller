using KIOSK.Presentation.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Presentation.Navigation.State
{
    public sealed class NavigationState
    {
        public IRootShellHost? RootShell { get; set; }

        // Shell: MenuShell, ExchangeShell, GtfShell 등
        public IShellHost? ActiveShell { get; set; }

        // FlowView: Shell 내부 화면
        public object? ActiveFlowView { get; set; }

        // DI 스코프들 (쉘 / 플로우)
        public IServiceScope? ShellScope { get; set; }
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

            ShellScope?.Dispose();
            ShellScope = null;

            ActiveFlowView = null;
            ActiveShell = null;
        }
    }
}
