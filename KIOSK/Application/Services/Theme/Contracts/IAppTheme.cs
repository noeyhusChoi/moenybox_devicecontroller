namespace Kiosk.Application.Services.Theme;

public enum AppThemeKind
{
    Light,
    LightRed,
    LightOrange,
    LightGreen,
    Black
}

public interface IAppTheme
{
    AppThemeKind CurrentTheme { get; }

    event EventHandler? ThemeChanged;

    void SetTheme(AppThemeKind theme);

    void ToggleTheme();
}
