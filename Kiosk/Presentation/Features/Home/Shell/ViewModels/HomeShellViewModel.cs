using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Services.Resx;

namespace Kiosk.ViewModels;

public partial class HomeShellViewModel : ObservableObject, IModalSourceViewModel
{
    public event EventHandler? ExchangeRequested;

    public HomeShellViewModel(IAppCulture appCulture)
    {
        HomeScreen = new HomeScreenViewModel(new AsyncRelayCommand(RequestExchangeAsync), appCulture);
        CurrentScreenViewModel = HomeScreen;
    }

    public HomeScreenViewModel HomeScreen { get; }

    [ObservableProperty]
    private object? currentScreenViewModel;

    public object? CurrentModalViewModel => null;

    private Task RequestExchangeAsync()
    {
        ExchangeRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
