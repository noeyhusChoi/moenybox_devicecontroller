using KIOSK.Presentation.Shared.Abstractions;
using System;

namespace KIOSK.Presentation.Navigation.Services
{
    public interface IPopupService
    { 
        // Popup (Shell 내부)
        void ShowPopup<TViewModel>(Action<TViewModel>? init = null)
            where TViewModel : class;

        void ClosePopup();
    }

    public sealed class PopupService : IPopupService
    {
        private readonly INavigationService _nav;

        public PopupService(INavigationService nav)
        {
            _nav = nav;
        }

        // POPUP (Shell)
        public void ShowPopup<T>(Action<T>? init = null)
            where T : class
        {
            if (_nav.ActiveShell == null)
                return;

            var vm = _nav.GetShellViewModel<T>();
            init?.Invoke(vm);

            if (_nav.ActiveShell is IPopup host)
                host.PopupContent = vm;
        }

        public void ClosePopup()
        {
            if (_nav.ActiveShell is IPopup host)
                host.PopupContent = null;
        }
    }
}
