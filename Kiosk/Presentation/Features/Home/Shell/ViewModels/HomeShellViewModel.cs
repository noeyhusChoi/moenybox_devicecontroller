using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Services.Resx;
using System.Globalization;

namespace Kiosk.ViewModels;

public sealed class HomeServiceEntryRequestedEventArgs : EventArgs
{
    public HomeServiceEntryRequestedEventArgs(HomeServiceType serviceType)
    {
        ServiceType = serviceType;
    }

    public HomeServiceType ServiceType { get; }
}

public partial class HomeShellViewModel : ObservableObject, IModalSourceViewModel
{
    private const string DefaultLanguageCode = "ko-KR";

    private readonly IAppCulture _appCulture;

    public event EventHandler<HomeServiceEntryRequestedEventArgs>? ServiceEntryRequested;

    public HomeShellViewModel(IAppCulture appCulture)
    {
        _appCulture = appCulture;
        HomeScreen = new HomeScreenViewModel(
            new AsyncRelayCommand(() => RequestServiceEntryAsync(HomeServiceType.TransportationCard)),
            new AsyncRelayCommand(() => RequestServiceEntryAsync(HomeServiceType.Exchange)),
            new AsyncRelayCommand(() => RequestServiceEntryAsync(HomeServiceType.TaxRefund)));
        ResetToServiceSelection();
    }

    public HomeScreenViewModel HomeScreen { get; }

    [ObservableProperty]
    private object? currentScreenViewModel;

    public object? CurrentModalViewModel => null;

    public void ResetToServiceSelection()
    {
        _appCulture.SetCulture(CultureInfo.GetCultureInfo(DefaultLanguageCode));
        CurrentScreenViewModel = HomeScreen;
    }

    private Task RequestServiceEntryAsync(HomeServiceType serviceType)
    {
        ServiceEntryRequested?.Invoke(this, new HomeServiceEntryRequestedEventArgs(serviceType));
        return Task.CompletedTask;
    }
}
