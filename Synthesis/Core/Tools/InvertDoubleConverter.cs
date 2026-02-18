using System.Globalization;
using System.Windows.Data;

namespace Synthesis.Core.Tools;

public class InvertDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double num)
        {
            return 0.0 - num;
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double num)
        {
            return 0.0 - num;
        }
        return 0.0;
    }
}
