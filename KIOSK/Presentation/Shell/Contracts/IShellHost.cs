using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Shell.Contracts
{
    public interface IShellHost : INavigable, IPopupHost
    {
        object? CurrentView { get; }

        // Shell 내부에 FlowView를 셋팅
        void SetInnerView(object view);
    }
}
