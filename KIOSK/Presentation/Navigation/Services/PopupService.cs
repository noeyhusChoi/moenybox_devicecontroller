using KIOSK.Presentation.Abstractions;
using System;

namespace KIOSK.Presentation.Navigation.Services
{
    public interface IPopupService
    { 
        // Popup (Layout 내부)
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

        // POPUP (Layout)
        public void ShowPopup<T>(Action<T>? init = null)
            where T : class
        {
            if (_nav.ActiveLayout is not IPopup host)
                return;

            var vm = _nav.GetLayoutViewModel<T>();
            init?.Invoke(vm);

            host.PopupContent = vm;
        }

        public void ClosePopup()
        {
            if (_nav.ActiveLayout is IPopup host)
                host.PopupContent = null;
        }
    }
}
