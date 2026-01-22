using KIOSK.Application.Services;
using KIOSK.Presentation.Navigation.State;
using KIOSK.Presentation.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace KIOSK.Presentation.Navigation.Popup
{
    public interface IPopupService
    {
        // Global Popup
        void ShowGlobal<TViewModel>(Action<TViewModel>? init = null)
            where TViewModel : class;

        void CloseGlobal();

        // Local Popup (Shell 내부)
        void ShowLocal<TViewModel>(Action<TViewModel>? init = null)
            where TViewModel : class;

        void CloseLocal();

        // TODO:Shell 전환 / Flow교체 시 사용
        void CloseAll();
    }

    public sealed class PopupService : IPopupService
    {
        private readonly NavigationState _state;

        public PopupService(NavigationState state)
        {
            _state = state;
        }

        // GLOBAL POPUP (RootShell or Shell)
        public void ShowGlobal<T>(Action<T>? init = null)
            where T : class
        {
            var host = GetGlobalHost();
            if (host == null)
                return;

            if (!ReferenceEquals(host, _state.ActiveShell) && _state.ActiveShell is IPopupHost localHost)
                localHost.PopupContent = null;

            var vm = _state.ShellScope?.ServiceProvider.GetService<T>()
                     ?? ActivatorUtilities.CreateInstance<T>(_state.ShellScope?.ServiceProvider!);

            init?.Invoke(vm);

            host.PopupContent = vm;
        }

        public void CloseGlobal()
        {
            var host = GetGlobalHost();
            if (host == null)
                return;

            host.PopupContent = null;
        }

        // LOCAL POPUP (Shell)
        public void ShowLocal<T>(Action<T>? init = null)
            where T : class
        {
            if (_state.ActiveShell == null)
                return;

            // Global Popup이 열려 있으면 금지
            if (GetGlobalHost()?.PopupContent != null)
                return;

            var vm = _state.ShellScope!.ServiceProvider.GetRequiredService<T>();
            init?.Invoke(vm);

            if (_state.ActiveShell is IPopupHost host)
                host.PopupContent = vm;
        }

        public void CloseLocal()
        {
            if (_state.ActiveShell is IPopupHost host)
                host.PopupContent = null;
        }

        // 모든 팝업 제거
        public void CloseAll()
        {
            if (_state.ActiveShell is IPopupHost l)
                l.PopupContent = null;

            if (GetGlobalHost() is IPopupHost g && !ReferenceEquals(g, _state.ActiveShell))
                g.PopupContent = null;
        }

        private IPopupHost? GetGlobalHost()
        {
            if (_state.RootShell is IPopupHost rootPopup)
                return rootPopup;

            return _state.ActiveShell as IPopupHost;
        }
    }
}
