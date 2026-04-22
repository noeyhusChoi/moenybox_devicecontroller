namespace Kiosk.Application.Services.Theme;

public enum AppThemeKind
{
    Light,
    HighContrast
}

public interface IAppTheme
{
    AppThemeKind CurrentTheme { get; }

    event EventHandler? ThemeChanged;

    void SetTheme(AppThemeKind theme);

    void ToggleTheme();
}
