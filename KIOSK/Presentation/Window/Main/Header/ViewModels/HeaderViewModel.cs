using CommunityToolkit.Mvvm.ComponentModel;
using Kiosk.Application.Services.Time;
using System.Windows;

namespace Kiosk.ViewModels;

public enum HeaderRightMode
{
    None,
    DateTime,
    Timer
}

public partial class HeaderViewModel : ObservableObject, IDisposable
{
    private readonly IClockService? _clockService;

    public HeaderViewModel(IClockService? clockService = null)
    {
        _clockService = clockService;

        if (_clockService is null)
            return;

        _clockService.TimeChanged += OnClockTimeChanged;
        ApplyCurrentTime(_clockService.Now);
    }

    [ObservableProperty]
    private string logoAssetPath = "pack://application:,,,/Assets/Image/LOGO_CI.png";

    [ObservableProperty]
    private HeaderRightMode rightMode = HeaderRightMode.DateTime;

    [ObservableProperty]
    private string? currentDateText;

    [ObservableProperty]
    private string? currentTimeText;

    [ObservableProperty]
    private string? timerText;

    public void Dispose()
    {
        if (_clockService is not null)
            _clockService.TimeChanged -= OnClockTimeChanged;
    }

    private void OnClockTimeChanged(object? sender, DateTime now)
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            ApplyCurrentTime(now);
            return;
        }

        if (System.Windows.Application.Current?.Dispatcher is not null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => ApplyCurrentTime(now));
            return;
        }

        ApplyCurrentTime(now);
    }

    private void ApplyCurrentTime(DateTime now)
    {
        if (RightMode != HeaderRightMode.DateTime)
            return;

        CurrentDateText = now.ToString("yyyy.MM.dd");
        CurrentTimeText = now.ToString("HH:mm:ss");
    }
}
