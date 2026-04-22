using System.ComponentModel;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace Kiosk.Application.Services.Resx;

internal sealed class ResxDesignTimeProvider : INotifyPropertyChanged
{
    private static readonly ConcurrentDictionary<string, ResxDesignTimeProvider> Cache = new();
    private static readonly ResxLocalizationOptions Options = new();
    private readonly ResourceManager _resourceManager;
    private readonly CultureInfo _culture;
    private readonly CultureInfo _defaultCulture;

    public static ResxDesignTimeProvider Instance => ForCulture(CultureInfo.CurrentUICulture);

    public static ResxDesignTimeProvider ForCulture(CultureInfo culture)
    {
        var normalizedCulture = NormalizeCulture(culture);
        return Cache.GetOrAdd(normalizedCulture.Name, _ => new ResxDesignTimeProvider(normalizedCulture));
    }

    private ResxDesignTimeProvider(CultureInfo culture)
    {
        _resourceManager = new ResourceManager(Options.BaseName, typeof(ResxDesignTimeProvider).Assembly);
        _culture = culture;
        _defaultCulture = CultureInfo.GetCultureInfo(Options.DefaultCultureName);
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }

    public string this[string key]
    {
        get
        {
            var value = _resourceManager.GetString(key, _culture);
            if (value == null && _culture.Name != _defaultCulture.Name)
            {
                value = _resourceManager.GetString(key, _defaultCulture);
            }

            return value ?? $"[{key}]";
        }
    }

    private static CultureInfo NormalizeCulture(CultureInfo culture)
    {
        foreach (var supportedCulture in Options.SupportedCultures)
        {
            if (string.Equals(supportedCulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                return supportedCulture;
            }
        }

        foreach (var supportedCulture in Options.SupportedCultures)
        {
            if (string.Equals(supportedCulture.TwoLetterISOLanguageName, culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
            {
                return supportedCulture;
            }
        }

        return CultureInfo.GetCultureInfo(Options.DefaultCultureName);
    }
}
