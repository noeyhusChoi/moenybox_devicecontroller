using System.ComponentModel;

namespace Kiosk.Application.Services.Resx
{
    public sealed class ResxLocalizationProvider : INotifyPropertyChanged
    {
        private static ResxLocalizationProvider? _instance;
        private IResxLocalizationService? _svc;

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

                return $"[{key}]";
            }
        }

        private void Attach(IResxLocalizationService svc)
        {
            if (_svc != null)
            {
                _svc.LanguageChanged -= OnLanguageChanged;
            }

            _svc = svc;
            _svc.LanguageChanged += OnLanguageChanged;
            RaiseIndexerChanged();
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            RaiseIndexerChanged();
        }

        private void RaiseIndexerChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
