using System.Diagnostics;
using KIOSK.Application.Abstractions;
using KIOSK.Infrastructure.Logging;
using KIOSK.Presentation.Navigation.State;
using KIOSK.Presentation.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Presentation.Navigation.Services;

public interface INavigationService
{
    void AttachRootShell(IWindowHost shell);

    // Shell 전환 (ServiceShell, ExchangeShell, GtfShell)
    Task SwitchShell<TShell>()
        where TShell : class, IShellHost;

    // Flow 전환
    Task NavigateTo<TView>(Action<TView>? init = null, object? parameter = null)
        where TView : class;

    T GetViewModel<T>() where T : class;

    IShellHost? ActiveShell { get; }

    object? ActiveFlowView { get; }
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    private readonly ILoggingService _logging;
    private readonly NavigationState _state;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly SemaphoreSlim _navigateLock = new(1, 1);

    public NavigationService(
        IServiceProvider provider,
        ILoggingService logging,
        NavigationState navState,
        IUiDispatcher uiDispatcher)
    {
        _provider = provider;
        _logging = logging;
        _state = navState;
        _uiDispatcher = uiDispatcher;
    }
    public IShellHost? ActiveShell => _state.ActiveShell;
    public object? ActiveFlowView => _state.ActiveFlowView;

    public void AttachRootShell(IWindowHost shell)
    {
        _state.RootShell = shell;
    }

    // Shell 전환
    public async Task SwitchShell<TShell>()
        where TShell : class, IShellHost
    {
        try
        {
            if (_state.RootShell == null)
                throw new InvalidOperationException("RootShell이 없습니다.");

            if (_state.ActiveShell is TShell currentShell)
            {
                var currentProvider = _state.ShellScope?.ServiceProvider ?? _provider;
                var candidate = currentProvider.GetRequiredService<TShell>();
                if (ReferenceEquals(currentShell, candidate))
                    return;
            }

            await TryUnloadAsync(_state.ActiveFlowView);
            await TryUnloadAsync(_state.ActiveShell);

            _state.FlowScope?.Dispose();
            _state.FlowScope = null;

            _state.ShellScope?.Dispose();
            _state.ShellScope = _provider.CreateScope();

            var sub = _state.ShellScope.ServiceProvider.GetRequiredService<TShell>();
            _state.ActiveShell = sub;

            await _uiDispatcher.InvokeAsync(() => _state.RootShell.SetShell(sub));

            if (sub is INavigable nav)
                await nav.OnLoadAsync(null, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
        }
    }

    // FlowView 전환
    public async Task NavigateTo<TView>(Action<TView>? init = null, object? parameter = null)
        where TView : class
    {
        await _navigateLock.WaitAsync();
        try
        {
            if (_state.ActiveShell == null)
                throw new InvalidOperationException("Shell이 없습니다.");

            if (_state.ActiveFlowView is TView currentFlowView && ReferenceEquals(_state.ActiveFlowView, currentFlowView))
                return;

            await TryUnloadAsync(_state.ActiveFlowView);

            _state.FlowCancellation?.Cancel();
            _state.FlowCancellation?.Dispose();

            _state.FlowScope?.Dispose();
            var flowRoot = _state.ShellScope?.ServiceProvider ?? _provider;
            _state.FlowScope = flowRoot.CreateScope();

            var vm = _state.FlowScope.ServiceProvider.GetRequiredService<TView>();
            init?.Invoke(vm);

            _state.ActiveFlowView = vm;
            await _uiDispatcher.InvokeAsync(() => _state.ActiveShell.SetInnerView(vm));

            _state.FlowCancellation = new CancellationTokenSource();

            if (vm is INavigable nav)
                await nav.OnLoadAsync(parameter, _state.FlowCancellation.Token);
        }
        finally
        {
            _navigateLock.Release();
        }
    }

    public T GetViewModel<T>() where T : class =>
        _provider.GetRequiredService<T>();

    private async Task TryUnloadAsync(object? target)
    {
        if (target is INavigable nav)
        {
            try
            {
                await nav.OnUnloadAsync();
            }
            catch (Exception ex)
            {
                _logging.Warn($"OnUnloadAsync failed: {ex.Message}");
            }
        }
    }

}

