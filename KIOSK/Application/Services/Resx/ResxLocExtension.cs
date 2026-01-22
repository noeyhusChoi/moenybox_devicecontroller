using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace Localization.Resx
{
    [MarkupExtensionReturnType(typeof(BindingExpression))]
    public class ResxLocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public ResxLocExtension() { }
        public ResxLocExtension(string key) => Key = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = ResxLocalizationProvider.Instance,
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
