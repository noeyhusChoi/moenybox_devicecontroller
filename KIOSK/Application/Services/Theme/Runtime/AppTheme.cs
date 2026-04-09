namespace Kiosk.Application.Services.Theme;

public sealed class AppTheme : IAppTheme
{
    private static readonly Uri LightThemeUri = new("/Resources/Colors/Colors.Semantic.Light.xaml", UriKind.Relative);
    private static readonly Uri LightRedThemeUri = new("/Resources/Colors/Colors.Semantic.LightRed.xaml", UriKind.Relative);
    private static readonly Uri LightOrangeThemeUri = new("/Resources/Colors/Colors.Semantic.LightOrange.xaml", UriKind.Relative);
    private static readonly Uri LightGreenThemeUri = new("/Resources/Colors/Colors.Semantic.LightGreen.xaml", UriKind.Relative);
    private static readonly Uri BlackThemeUri = new("/Resources/Colors/Colors.Semantic.Black.xaml", UriKind.Relative);

    public AppThemeKind CurrentTheme { get; private set; } = AppThemeKind.Light;

    public event EventHandler? ThemeChanged;

    public void SetTheme(AppThemeKind theme)
    {
        if (System.Windows.Application.Current is null)
            return;

        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var themeDictionary = new System.Windows.ResourceDictionary
        {
            Source = theme switch
            {
                AppThemeKind.LightRed => LightRedThemeUri,
                AppThemeKind.LightOrange => LightOrangeThemeUri,
                AppThemeKind.LightGreen => LightGreenThemeUri,
                AppThemeKind.Black => BlackThemeUri,
                _ => LightThemeUri
            }
        };

        var index = -1;
        for (var i = 0; i < dictionaries.Count; i++)
        {
            var source = dictionaries[i].Source?.OriginalString;
            if (source is null)
                continue;

            if (source.EndsWith("Colors.Semantic.Current.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Colors.Semantic.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Colors.Semantic.LightRed.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Colors.Semantic.LightOrange.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Colors.Semantic.LightGreen.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Colors.Semantic.Black.xaml", StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
            dictionaries[index] = themeDictionary;
        else
            dictionaries.Add(themeDictionary);

        CurrentTheme = theme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == AppThemeKind.Light ? AppThemeKind.LightOrange : AppThemeKind.Light);
    }
}
