using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Documents;

namespace Kiosk.Application.Services.Resx
{
    [MarkupExtensionReturnType(typeof(BindingExpression))]
    public class ResxLocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public ResxLocExtension() { }
        public ResxLocExtension(string key) => Key = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var targetService = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            var targetObject = targetService?.TargetObject;

            object source;
            if (IsInDesignMode(targetObject))
            {
                source = ResxDesignTimeProvider.ForCulture(GetDesignCulture(targetObject));
            }
            else
            {
                source = ResxLocalizationProvider.Instance;
            }

            var binding = new Binding($"[{Key}]")
            {
                Source = source,
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };

            return binding.ProvideValue(serviceProvider);
        }

        private static bool IsInDesignMode(object? targetObject)
        {
            return targetObject switch
            {
                DependencyObject dependencyObject => DesignerProperties.GetIsInDesignMode(dependencyObject),
                _ => DesignerProperties.GetIsInDesignMode(new DependencyObject())
            };
        }

        private static CultureInfo GetDesignCulture(object? targetObject)
        {
            var ietfTag = targetObject switch
            {
                FrameworkElement element when !string.IsNullOrWhiteSpace(element.Language?.IetfLanguageTag) => element.Language.IetfLanguageTag,
                FrameworkContentElement contentElement when !string.IsNullOrWhiteSpace(contentElement.Language?.IetfLanguageTag) => contentElement.Language.IetfLanguageTag,
                _ => CultureInfo.CurrentUICulture.Name
            };

            try
            {
                return CultureInfo.GetCultureInfo(ietfTag);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.CurrentUICulture;
            }
        }
    }
}
