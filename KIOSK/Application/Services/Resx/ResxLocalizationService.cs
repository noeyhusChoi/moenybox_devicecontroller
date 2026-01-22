using KIOSK.Application.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace Localization.Resx
{
    public interface IResxLocalizationService
    {
        CultureInfo CurrentCulture { get; }
        IReadOnlyList<CultureInfo> SupportedCultures { get; }

        void SetCulture(CultureInfo culture);
        string? GetString(string key);

        event EventHandler? LanguageChanged;
    }

    // ResxLocalizationService: resx 기반 런타임/디자인타임 공용 로직
    public sealed class ResxLocalizationService : IResxLocalizationService
    {
        private readonly ILoggingService _logging;
        private readonly ResxLocalizationOptions _options;
        private readonly ResourceManager _resourceManager;
        private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler? LanguageChanged;

        public IReadOnlyList<CultureInfo> SupportedCultures => _options.SupportedCultures;

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

        public ResxLocalizationService(
            ILoggingService logging,
            IOptions<ResxLocalizationOptions> options,
            CultureInfo? initialCulture = null)
        {
            _logging = logging;
            _options = options?.Value ?? new ResxLocalizationOptions();
            _resourceManager = new ResourceManager(_options.BaseName, typeof(ResxLocalizationService).Assembly);
            CurrentCulture = initialCulture ?? CultureInfo.GetCultureInfo(_options.DefaultCultureName);
            ApplyThreadCulture(CurrentCulture);
        }

        public void SetCulture(CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            if (culture.Name == CurrentCulture.Name) return;

            CurrentCulture = culture;
            _cache.Clear();
            ApplyThreadCulture(culture);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public string? GetString(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (_cache.TryGetValue(key, out var cached)) return cached;

            string? value = null;
            try
            {
                value = _resourceManager.GetString(key, CurrentCulture);
            }
            catch (MissingManifestResourceException ex)
            {
                _logging?.Debug($"Resx resource load failed: {ex.Message}");
            }

            if (value != null) _cache[key] = value;
            return value;
        }

        private static void ApplyThreadCulture(CultureInfo culture)
        {
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
        }
    }
}
