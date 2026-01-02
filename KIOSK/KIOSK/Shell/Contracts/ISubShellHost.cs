using KIOSK.ViewModels;

namespace KIOSK.Shell.Contracts
{
    public interface ISubShellHost : INavigable, IPopupHost
    {
        object? CurrentView { get; }

        // SubShell 내부에 FlowView를 셋팅
        void SetInnerView(object view);
    }
}
