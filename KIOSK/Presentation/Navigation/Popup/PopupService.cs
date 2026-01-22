using KIOSK.Application.Services;
using KIOSK.Presentation.Navigation.State;
using KIOSK.Presentation.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace KIOSK.Presentation.Navigation.Popup
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
        private readonly NavigationState _state;

        public PopupService(NavigationState state)
        {
            _state = state;
        }

        // POPUP (Shell)
        public void ShowPopup<T>(Action<T>? init = null)
            where T : class
        {
            if (_state.ActiveShell == null)
                return;

            var vm = _state.ShellScope!.ServiceProvider.GetRequiredService<T>();
            init?.Invoke(vm);

            if (_state.ActiveShell is IPopupHost host)
                host.PopupContent = vm;
        }

        public void ClosePopup()
        {
            if (_state.ActiveShell is IPopupHost host)
                host.PopupContent = null;
        }
    }
}
