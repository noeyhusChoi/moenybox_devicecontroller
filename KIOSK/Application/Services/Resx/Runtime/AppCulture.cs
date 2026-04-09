using Kiosk.Application.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;

namespace Kiosk.Application.Services.Resx;

public sealed class AppCulture : IAppCulture
{
    private readonly ILoggingService _logging;
    private readonly ResxLocalizationOptions _options;
    private readonly CultureInfo _defaultCulture;

    public AppCulture(
        ILoggingService logging,
        IOptions<ResxLocalizationOptions> options,
        CultureInfo? initialCulture = null)
    {
        _logging = logging;
        _options = options?.Value ?? new ResxLocalizationOptions();
        _defaultCulture = CultureInfo.GetCultureInfo(_options.DefaultCultureName);
        CurrentCulture = NormalizeCulture(initialCulture ?? _defaultCulture);
        ApplyThreadCulture(CurrentCulture);
    }

    public CultureInfo CurrentCulture { get; private set; }

    public IReadOnlyList<CultureInfo> SupportedCultures => _options.SupportedCultures;

    public event EventHandler? CultureChanged;

    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var normalizedCulture = NormalizeCulture(culture);
        if (normalizedCulture.Name == CurrentCulture.Name)
        {
            return;
        }

        CurrentCulture = normalizedCulture;
        ApplyThreadCulture(normalizedCulture);
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ApplyThreadCulture(CultureInfo culture)
    {
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }

    private CultureInfo NormalizeCulture(CultureInfo culture)
    {
        foreach (var supportedCulture in _options.SupportedCultures)
        {
            if (string.Equals(supportedCulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                return supportedCulture;
            }
        }

        foreach (var supportedCulture in _options.SupportedCultures)
        {
            if (string.Equals(
                supportedCulture.TwoLetterISOLanguageName,
                culture.TwoLetterISOLanguageName,
                StringComparison.OrdinalIgnoreCase))
            {
                return supportedCulture;
            }
        }

        _logging?.Debug(
            $"Unsupported app culture '{culture.Name}'. Falling back to default '{_defaultCulture.Name}'.");
        return _defaultCulture;
    }
}
