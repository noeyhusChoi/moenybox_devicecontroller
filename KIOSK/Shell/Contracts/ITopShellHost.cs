using KIOSK.ViewModels;

namespace KIOSK.Shell.Contracts
{
    public interface ITopShellHost : INavigable, IPopupHost
    {
        // TopShell 내부에서 현재 어떤 SubShell이 활성인지
        object? CurrentSubShell { get; }

        // TopShell 내부에 SubShell을 붙인다
        void SetSubShell(object? shell);
    }
}
