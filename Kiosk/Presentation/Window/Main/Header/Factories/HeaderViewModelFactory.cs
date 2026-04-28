using Kiosk.Application.Services.Time;
using Kiosk.Application.Services.Theme;

namespace Kiosk.ViewModels;

public sealed class HeaderViewModelFactory : IHeaderViewModelFactory
{
    private readonly IClockService _clockService;
    private readonly IAppTheme _appTheme;

    public HeaderViewModelFactory(IClockService clockService, IAppTheme appTheme)
    {
        _clockService = clockService;
        _appTheme = appTheme;
    }

    public string GetLogoAssetPath()
        => _appTheme.CurrentTheme == AppThemeKind.Black
            ? "pack://application:,,,/Assets/Image/LOGO_CI_white.png"
            : "pack://application:,,,/Assets/Image/LOGO_CI_black.png";

    public HeaderViewModel CreateHomeHeader()
        => new(_clockService)
        {
            LogoAssetPath = GetLogoAssetPath(),
            RightMode = HeaderRightMode.DateTime,
            CurrentDateText = _clockService.Now.ToString("yyyy.MM.dd"),
            CurrentTimeText = _clockService.Now.ToString("HH:mm"),
            TimerText = null
        };

    public HeaderViewModel CreateExchangeHeader(string? timerText)
        => new(_clockService)
        {
            LogoAssetPath = GetLogoAssetPath(),
            RightMode = HeaderRightMode.Timer,
            CurrentDateText = null,
            CurrentTimeText = null,
            TimerText = timerText
        };
}
