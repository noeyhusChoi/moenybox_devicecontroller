using System.Diagnostics;
using KIOSK.Application.Abstractions;
using KIOSK.Infrastructure.Logging;
using KIOSK.Presentation.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KIOSK.Presentation.Navigation.Services;

public interface INavigationService
{
    void SetRootWindow(IWindow window);

    // Layout 전환 (ServiceShell, ExchangeShell, GtfShell 등)
    Task NavigateLayout<TLayout>()
        where TLayout : class, ILayout;

    // Page 전환
    Task NavigatePage<TPage>(Action<TPage>? init = null, object? parameter = null)
        where TPage : class;

    T GetViewModel<T>() where T : class;
    T GetLayoutViewModel<T>() where T : class;

    ILayout? ActiveLayout { get; }

    object? ActivePage { get; }
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    private readonly ILoggingService _logging;

    private readonly IUiDispatcher _uiDispatcher;
    private readonly SemaphoreSlim _navigateLock = new(1, 1);

    private IWindow? _rootWindow;
    private ILayout? _activeLayout;
    private object? _activePage;

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
    public ILayout? ActiveLayout => _activeLayout;
    public object? ActivePage => _activePage;

    public void SetRootWindow(IWindow window)
    {
        _rootWindow = window;
    }

    // Layout 전환
    public async Task NavigateLayout<TLayout>()
        where TLayout : class, ILayout
    {
        try
        {
            var rootWindow = _rootWindow ?? throw new InvalidOperationException("RootWindow가 없습니다.");

            if (_activeLayout is TLayout currentLayout)
            {
                var currentProvider = _shellScope?.ServiceProvider ?? _provider;
                var candidate = currentProvider.GetRequiredService<TLayout>();
                if (ReferenceEquals(currentLayout, candidate))
                    return;
            }

            await TryUnloadAsync(_activePage);
            await TryUnloadAsync(_activeLayout);

            _flowScope?.Dispose();
            _flowScope = null;

            _shellScope?.Dispose();
            _shellScope = _provider.CreateScope();

            var layout = _shellScope.ServiceProvider.GetRequiredService<TLayout>();

            await _uiDispatcher.InvokeAsync(() =>
            {
                _activeLayout = layout;
                _activePage = null;
                rootWindow.CurrentLayout = layout;
            });

            if (layout is INavigable nav)
                await nav.OnLoadAsync(null, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
        }
    }

    // Page 전환
    public async Task NavigatePage<TView>(Action<TView>? init = null, object? parameter = null)
        where TView : class
    {
        await _navigateLock.WaitAsync();
        try
        {
            var activeLayout = _activeLayout ?? throw new InvalidOperationException("Layout이 없습니다.");

            if (_activePage is TView currentPage && ReferenceEquals(_activePage, currentPage))
                return;

            await TryUnloadAsync(_activePage);

            _flowCancellation?.Cancel();
            _flowCancellation?.Dispose();

            _flowScope?.Dispose();
            var flowRoot = _shellScope?.ServiceProvider ?? _provider;
            _flowScope = flowRoot.CreateScope();

            var vm = _flowScope.ServiceProvider.GetRequiredService<TView>();
            init?.Invoke(vm);

            await _uiDispatcher.InvokeAsync(() =>
            {
                _activePage = vm;
                activeLayout.CurrentPage = vm;
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

    public T GetLayoutViewModel<T>() where T : class
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

