using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Shell.Contracts
{
    public interface ITopShellHost : INavigable, IPopupHost
    {
        // TopShell 내부에서 현재 어떤 Shell이 활성인지
        object? CurrentShell { get; }

        // TopShell 내부에 Shell을 붙인다
        void SetShell(object? shell);
    }
}
