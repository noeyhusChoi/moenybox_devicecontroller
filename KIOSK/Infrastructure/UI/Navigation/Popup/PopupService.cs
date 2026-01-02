using KIOSK.Infrastructure.UI.Navigation.State;
using KIOSK.Shell.Contracts;
using KIOSK.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace KIOSK.Infrastructure.UI.Navigation
{
    public interface IPopupService
    {
        // Global Popup
        void ShowGlobal<TViewModel>(Action<TViewModel>? init = null)
            where TViewModel : class;

        void CloseGlobal();

        // Local Popup (SubShell 내부)
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

        // GLOBAL POPUP (TopShell)
        public void ShowGlobal<T>(Action<T>? init = null)
            where T : class
        {
            if (_state.ActiveTopShell == null)
                return;

            // Local Popup 제거
            if (_state.ActiveSubShell is IPopupHost localHost)
                localHost.PopupContent = null;

            var vm = _state.SubShellScope?.ServiceProvider.GetService<T>()
                     ?? ActivatorUtilities.CreateInstance<T>(_state.SubShellScope?.ServiceProvider!);

            init?.Invoke(vm);

            _state.ActiveTopShell.PopupContent = vm;
        }

        public void CloseGlobal()
        {
            if (_state.ActiveTopShell == null)
                return;

            _state.ActiveTopShell.PopupContent = null;
        }

        // LOCAL POPUP (SubShell)
        public void ShowLocal<T>(Action<T>? init = null)
            where T : class
        {
            if (_state.ActiveSubShell == null)
                return;

            // Global Popup이 열려 있으면 금지
            if (_state.ActiveTopShell?.PopupContent != null)
                return;

            var vm = _state.SubShellScope!.ServiceProvider.GetRequiredService<T>();
            init?.Invoke(vm);

            if (_state.ActiveSubShell is IPopupHost host)
                host.PopupContent = vm;
        }

        public void CloseLocal()
        {
            if (_state.ActiveSubShell is IPopupHost host)
                host.PopupContent = null;
        }

        // 모든 팝업 제거
        public void CloseAll()
        {
            if (_state.ActiveTopShell is IPopupHost g)
                g.PopupContent = null;

            if (_state.ActiveSubShell is IPopupHost l)
                l.PopupContent = null;
        }
    }
}
