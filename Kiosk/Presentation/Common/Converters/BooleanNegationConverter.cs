using System.Globalization;
using System.Windows.Data;

namespace Kiosk.Converters;

public sealed class BooleanNegationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue && !boolValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue && !boolValue;
}
