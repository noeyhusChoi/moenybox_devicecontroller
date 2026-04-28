using Kiosk.Application.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace Kiosk.Application.Services.Resx
{
    public sealed class ResxLocalizationService : IResxLocalizationService
    {
        private readonly ILoggingService _logging;
        private readonly IAppCulture _appCulture;
        private readonly ResourceManager _resourceManager;
        private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly CultureInfo _defaultCulture;

        public event EventHandler? LanguageChanged;

        public ResxLocalizationService(
            ILoggingService logging,
            IOptions<ResxLocalizationOptions> options,
            IAppCulture appCulture)
        {
            _logging = logging;
            _appCulture = appCulture;

            var resolvedOptions = options?.Value ?? new ResxLocalizationOptions();
            _resourceManager = new ResourceManager(resolvedOptions.BaseName, typeof(ResxLocalizationService).Assembly);
            _defaultCulture = CultureInfo.GetCultureInfo(resolvedOptions.DefaultCultureName);
            _appCulture.CultureChanged += OnCultureChanged;
        }

        public string? GetString(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (_cache.TryGetValue(key, out var cached)) return cached;

            string? value = null;
            try
            {
                value = _resourceManager.GetString(key, _appCulture.CurrentCulture);
                if (value == null && _appCulture.CurrentCulture.Name != _defaultCulture.Name)
                {
                    value = _resourceManager.GetString(key, _defaultCulture);
                }
            }
            catch (MissingManifestResourceException ex)
            {
                _logging?.Debug($"Resx resource load failed: {ex.Message}");
            }

            if (value != null) _cache[key] = value;
            return value;
        }

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            _cache.Clear();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
