using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Localization.Resx
{
    // MarkupExtension이 바인딩할 "단일 소스"
    public sealed class ResxLocalizationProvider : INotifyPropertyChanged
    {
        private static ResxLocalizationProvider? _instance;
        private IResxLocalizationService? _svc;
        private readonly ResourceManager _fallbackManager =
            new("KIOSK.Resources.Resx.StringResources", typeof(ResxLocalizationProvider).Assembly);

        public static ResxLocalizationProvider Instance => _instance ??= new ResxLocalizationProvider();
        public bool IsInitialized => _svc != null;

        private ResxLocalizationProvider() { }

        public static void Initialize(IResxLocalizationService svc)
        {
            Instance.Attach(svc);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key]
        {
            get
            {
                if (_svc != null)
                {
                    return _svc.GetString(key) ?? $"[{key}]";
                }

                var value = _fallbackManager.GetString(key, CultureInfo.CurrentUICulture);
                return value ?? $"[{key}]";
            }
        }

        private void Attach(IResxLocalizationService svc)
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
}
