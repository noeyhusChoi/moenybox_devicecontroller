using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiosk.Application.Services.Resx;
using System.Globalization;

namespace Kiosk.ViewModels;

public sealed class HomeServiceEntryRequestedEventArgs : EventArgs
{
    public HomeServiceEntryRequestedEventArgs(HomeServiceType serviceType, string languageCode)
    {
        ServiceType = serviceType;
        LanguageCode = languageCode;
    }

    public HomeServiceType ServiceType { get; }
    public string LanguageCode { get; }
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
            new AsyncRelayCommand(() => ShowLanguageSelectionAsync(HomeServiceType.TransportationCard)),
            new AsyncRelayCommand(() => ShowLanguageSelectionAsync(HomeServiceType.Exchange)),
            new AsyncRelayCommand(() => ShowLanguageSelectionAsync(HomeServiceType.TaxRefund)));
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

    private Task ShowLanguageSelectionAsync(HomeServiceType serviceType)
    {
        CurrentScreenViewModel = new HomeLanguageSelectionViewModel(
            serviceType,
            GetSupportedLanguageCodes(serviceType),
            RequestServiceEntry);

        return Task.CompletedTask;
    }

    private void RequestServiceEntry(HomeServiceType serviceType, string languageCode)
    {
        _appCulture.SetCulture(CultureInfo.GetCultureInfo(languageCode));
        ServiceEntryRequested?.Invoke(this, new HomeServiceEntryRequestedEventArgs(serviceType, languageCode));
    }

    private static IReadOnlyCollection<string> GetSupportedLanguageCodes(HomeServiceType serviceType)
    {
        return serviceType switch
        {
            HomeServiceType.Exchange =>
            [
                "ko-KR",
                "en-US",
                "ja-JP",
                "zh-CN",
                "zh-TW"
            ],
            HomeServiceType.TransportationCard =>
            [
                "ko-KR",
                "en-US",
                "ja-JP"
            ],
            HomeServiceType.TaxRefund =>
            [
                "ko-KR",
                "en-US"
            ],
            _ => [DefaultLanguageCode]
        };
    }
}
