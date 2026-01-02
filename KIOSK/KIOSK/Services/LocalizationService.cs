using KIOSK.Services;
using KIOSK.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Threading;

namespace Localization
{
    public interface ILocalizationService
    {
        CultureInfo CurrentCulture { get; }
        IReadOnlyList<CultureInfo> SupportedCultures { get; }

        void SetCulture(CultureInfo culture);
        string? GetString(string key);

        event EventHandler? LanguageChanged;
    }

    // LocalizationService: 런타임/디자인타임 공용 로직
    public class LocalizationService : ILocalizationService
    {
        private readonly ILoggingService _logging;
        private readonly LocalizationOptions _options;
        private readonly ResourceDictionary _langDictionary = new();
        private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ResourceDictionary> _dictionaryCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _gate = new(1, 1);

        public event EventHandler? LanguageChanged;

        public IReadOnlyList<CultureInfo> SupportedCultures => _options.SupportedCultures;

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

        public LocalizationService(
            ILoggingService logging,
            IOptions<LocalizationOptions> options,
            CultureInfo? initialCulture = null)
        {
            _logging = logging;
            _options = options?.Value ?? new LocalizationOptions();
            CurrentCulture = initialCulture ?? CultureInfo.GetCultureInfo(_options.DefaultCultureName);
            LoadForCulture(CurrentCulture).GetAwaiter().GetResult();
        }

        public void SetCulture(CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            if (culture.Name == CurrentCulture.Name) return;

            CurrentCulture = culture;
            LoadForCulture(culture).GetAwaiter().GetResult();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public string? GetString(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (_cache.TryGetValue(key, out var cached)) return cached;

            // ResourceDictionary 안전 조회
            string? value = null;
            if (_langDictionary.Contains(key))
            {
                value = _langDictionary[key] as string;
            }
            // 추가 폴백: App.Resources에도 있으면 사용
            else
            {
                var app = Application.Current;
                if (app != null)
                {
                    foreach (var rd in app.Resources.MergedDictionaries.Reverse())
                    {
                        if (rd.Contains(key))
                        {
                            value = rd[key] as string;
                            break;
                        }
                    }
                }
            }

            if (value != null) _cache[key] = value;
            return value;
        }

        private async Task LoadForCulture(CultureInfo culture)
        {
            _cache.Clear();

            ResourceDictionary? cached;
            if (_dictionaryCache.TryGetValue(culture.Name, out cached))
            {
                ApplyDictionary(cached);
                return;
            }

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_dictionaryCache.TryGetValue(culture.Name, out cached))
                {
                    ApplyDictionary(cached);
                    return;
                }

                var rd = LoadDictionaryForCulture(culture);

                // 폴백: TwoLetter, Default
                if (rd.MergedDictionaries.Count == 0)
                {
                    var two = new CultureInfo(culture.TwoLetterISOLanguageName);
                    rd = LoadDictionaryForCulture(two);
                }
                if (rd.MergedDictionaries.Count == 0 && _options.DefaultCultureName != culture.Name)
                {
                    var def = new CultureInfo(_options.DefaultCultureName);
                    rd = LoadDictionaryForCulture(def);
                }

                _dictionaryCache[culture.Name] = rd;
                ApplyDictionary(rd);
            }
            finally
            {
                _gate.Release();
            }
        }

        private ResourceDictionary LoadDictionaryForCulture(CultureInfo culture)
        {
            var rd = new ResourceDictionary();
            var candidates = new List<string>
            {
                Path.Combine(_options.BasePath, $"StringResources.{culture.Name}.xaml"),
                Path.Combine(_options.BasePath, $"StringResources.{culture.TwoLetterISOLanguageName}.xaml"),
                Path.Combine(_options.BasePath, "StringResources.xaml")
            };

            var assemblyName = typeof(LocalizationService).Assembly.GetName().Name ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            foreach (var c in candidates.Distinct())
            {
                try
                {
                    var uri = new Uri($"pack://application:,,,/{assemblyName};component/{c}", UriKind.Absolute);
                    rd.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
                }
                catch (Exception ex)
                {
                    _logging?.Debug($"Failed to load '{c}' -> {ex.GetType().Name}: {ex.Message}");
                }
            }

            return rd;
        }

        private void ApplyDictionary(ResourceDictionary rd)
        {
            _langDictionary.MergedDictionaries.Clear();
            foreach (var d in rd.MergedDictionaries)
                _langDictionary.MergedDictionaries.Add(d);

            var app = Application.Current;
            if (app != null)
            {
                void Merge()
                {
                    var appRDs = app.Resources.MergedDictionaries;
                    var oldLocals = appRDs
                        .Where(x => x.Source != null &&
                                    x.Source.OriginalString.IndexOf($"{_options.BasePath}/StringResources.", StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                    foreach (var o in oldLocals) appRDs.Remove(o);

                    foreach (var d in _langDictionary.MergedDictionaries)
                        appRDs.Add(d);
                }

                if (app.Dispatcher.CheckAccess())
                    Merge();
                else
                    app.Dispatcher.Invoke(Merge);
            }
        }
    }

    // MarkupExtension이 바인딩할 “단일 소스”
    public sealed class LocalizationProvider : INotifyPropertyChanged
    {
        private static LocalizationProvider? _instance;
        private ILocalizationService? _svc;

        public static LocalizationProvider Instance => _instance ??= new LocalizationProvider();
        public bool IsInitialized => _svc != null;

        private LocalizationProvider() { }

        public static void Initialize(ILocalizationService svc)
        {
            Instance.Attach(svc);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key] => _svc?.GetString(key) ?? $"[{key}]";

        private void Attach(ILocalizationService svc)
        {
            _svc = svc;
            _svc.LanguageChanged += (_, __) =>
            {
                // 인덱서 변경 알림 (Item[])
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            };
            // 초기 알림(디자이너에서 바인딩이 바로 반영되도록)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }

    [MarkupExtensionReturnType(typeof(BindingExpression))]
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public LocExtension() { }
        public LocExtension(string key) => Key = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationProvider.Instance,
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
