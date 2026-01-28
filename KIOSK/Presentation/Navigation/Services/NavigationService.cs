using System.Diagnostics;
using KIOSK.Application.Abstractions;
using KIOSK.Infrastructure.Logging;
using KIOSK.Presentation.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Presentation.Navigation.Services;

public interface INavigationService
{
    void SetRootHost(IWindow host);

    // Shell 전환 (ServiceShell, ExchangeShell, GtfShell)
    Task NavigateLayout<TLayout>()
        where TLayout : class, ILayout;

    // Flow 전환
    Task NavigatePage<TPage>(Action<TPage>? init = null, object? parameter = null)
        where TPage : class;

    T GetViewModel<T>() where T : class;
    T GetShellViewModel<T>() where T : class;

    ILayout? ActiveShell { get; }

    object? ActiveFlowView { get; }
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    private readonly ILoggingService _logging;

    private readonly IUiDispatcher _uiDispatcher;
    private readonly SemaphoreSlim _navigateLock = new(1, 1);

    private IWindow? _rootShell;
    private ILayout? _activeShell;
    private object? _activeFlowView;

    private IServiceScope? _shellScope;
    private IServiceScope? _flowScope;
    private CancellationTokenSource? _flowCancellation;

    public NavigationService(
        IServiceProvider provider,
        ILoggingService logging,
        IUiDispatcher uiDispatcher)
    {
        _provider = provider;
        _logging = logging;
        _uiDispatcher = uiDispatcher;
    }
    public ILayout? ActiveShell => _activeShell;
    public object? ActiveFlowView => _activeFlowView;

    public void SetRootHost(IWindow host)
    {
        _rootShell = host;
    }

    // Shell 전환
    public async Task NavigateLayout<TShell>()
        where TShell : class, ILayout
    {
        try
        {
            var rootShell = _rootShell ?? throw new InvalidOperationException("RootShell이 없습니다.");

            if (_activeShell is TShell currentShell)
            {
                var currentProvider = _shellScope?.ServiceProvider ?? _provider;
                var candidate = currentProvider.GetRequiredService<TShell>();
                if (ReferenceEquals(currentShell, candidate))
                    return;
            }

            await TryUnloadAsync(_activeFlowView);
            await TryUnloadAsync(_activeShell);

            _flowScope?.Dispose();
            _flowScope = null;

            _shellScope?.Dispose();
            _shellScope = _provider.CreateScope();

            var sub = _shellScope.ServiceProvider.GetRequiredService<TShell>();

            await _uiDispatcher.InvokeAsync(() =>
            {
                _activeShell = sub;
                _activeFlowView = null;
                rootShell.SetShell(sub);
            });

            if (sub is INavigable nav)
                await nav.OnLoadAsync(null, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
        }
    }

    // FlowView 전환
    public async Task NavigatePage<TView>(Action<TView>? init = null, object? parameter = null)
        where TView : class
    {
        await _navigateLock.WaitAsync();
        try
        {
            var activeShell = _activeShell ?? throw new InvalidOperationException("Shell이 없습니다.");

            if (_activeFlowView is TView currentFlowView && ReferenceEquals(_activeFlowView, currentFlowView))
                return;

            await TryUnloadAsync(_activeFlowView);

            _flowCancellation?.Cancel();
            _flowCancellation?.Dispose();

            _flowScope?.Dispose();
            var flowRoot = _shellScope?.ServiceProvider ?? _provider;
            _flowScope = flowRoot.CreateScope();

            var vm = _flowScope.ServiceProvider.GetRequiredService<TView>();
            init?.Invoke(vm);

            await _uiDispatcher.InvokeAsync(() =>
            {
                _activeFlowView = vm;
                activeShell.CurrentView = vm;
            });

            _flowCancellation = new CancellationTokenSource();

            if (vm is INavigable nav)
                await nav.OnLoadAsync(parameter, _flowCancellation.Token);
        }
        finally
        {
            _navigateLock.Release();
        }
    }

    public T GetViewModel<T>() where T : class =>
        _provider.GetRequiredService<T>();

    public T GetShellViewModel<T>() where T : class
    {
        var provider = _shellScope?.ServiceProvider ?? _provider;
        return provider.GetRequiredService<T>();
    }

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

