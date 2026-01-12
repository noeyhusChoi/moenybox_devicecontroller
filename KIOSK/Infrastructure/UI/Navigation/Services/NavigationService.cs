using System.Diagnostics;
using KIOSK.Presentation.Shell.Contracts;
using KIOSK.Infrastructure.Logging;
using KIOSK.Infrastructure.UI.Navigation.State;
using KIOSK.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Infrastructure.UI.Navigation.Services;

// ==========================================
// Public API
// ==========================================
public interface INavigationService
{
    void AttachRootShell(IRootShellHost shell);

    // TopShell 전환 (RootShell <-> EnvironmentShell)
    Task SwitchTopShell<TTopShell>()
        where TTopShell : class, ITopShellHost;

    // SubShell 전환 (ServiceShell, ExchangeShell, GtfShell)
    Task SwitchSubShell<TSubShell>()
        where TSubShell : class, ISubShellHost;

    // Flow 전환
    Task NavigateTo<TView>(Action<TView>? init = null, object? parameter = null)
        where TView : class;

    // 기존과 동일한 기본 기능
    T GetViewModel<T>() where T : class;

    ITopShellHost? ActiveTopShell { get; }
    ISubShellHost? ActiveSubShell { get; }
    object? ActiveFlowView { get; }
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    private readonly ILoggingService _logging;
    private readonly NavigationState _state;

    public NavigationService(
        IServiceProvider provider,
        ILoggingService logging,
        NavigationState navState)
    {
        _provider = provider;
        _logging = logging;
        _state = navState;
    }
    public ITopShellHost? ActiveTopShell => _state.ActiveTopShell;
    public ISubShellHost? ActiveSubShell => _state.ActiveSubShell;
    public object? ActiveFlowView => _state.ActiveFlowView;

    public void AttachRootShell(IRootShellHost shell)
    {
        _state.RootShell = shell;
    }

    // ------------------------------
    // 1. TopShell 전환
    // ------------------------------
    public async Task SwitchTopShell<TTopShell>()
        where TTopShell : class, ITopShellHost
    {
        _state.ResetAll();

        var shell = _provider.GetRequiredService<TTopShell>();
        _state.ActiveTopShell = shell;
        _state.RootShell.SetTopShell(shell);

        if (shell is INavigable nav)
            await nav.OnLoadAsync(null, CancellationToken.None);
    }

    // ------------------------------
    // 2. SubShell 전환
    // ------------------------------
    public async Task SwitchSubShell<TSubShell>()
        where TSubShell : class, ISubShellHost
    {
        try
        {

            if (_state.ActiveTopShell == null)
                throw new InvalidOperationException("TopShell이 없습니다.");

            _state.FlowScope?.Dispose();
            _state.FlowScope = null;

            _state.SubShellScope?.Dispose();
            _state.SubShellScope = _provider.CreateScope();

            var sub = _state.SubShellScope.ServiceProvider.GetRequiredService<TSubShell>();
            _state.ActiveSubShell = sub;

            _state.ActiveTopShell.SetSubShell(sub);

            if (sub is INavigable nav)
                await nav.OnLoadAsync(null, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
        }
    }

    // ------------------------------
    // 3. FlowView 전환
    // ------------------------------
    public async Task NavigateTo<TView>(Action<TView>? init = null, object? parameter = null)
        where TView : class
    {
        if (_state.ActiveSubShell == null)
            throw new InvalidOperationException("SubShell이 없습니다.");

        _state.FlowCancellation?.Cancel();
        _state.FlowCancellation?.Dispose();

        _state.FlowScope?.Dispose();
        _state.FlowScope = _provider.CreateScope();

        var vm = _state.FlowScope.ServiceProvider.GetRequiredService<TView>();
        init?.Invoke(vm);

        _state.ActiveFlowView = vm;
        _state.ActiveSubShell.SetInnerView(vm);

        _state.FlowCancellation = new CancellationTokenSource();

        if (vm is INavigable nav)
            await nav.OnLoadAsync(parameter, _state.FlowCancellation.Token);
    }

    public T GetViewModel<T>() where T : class =>
        _provider.GetRequiredService<T>();
}

