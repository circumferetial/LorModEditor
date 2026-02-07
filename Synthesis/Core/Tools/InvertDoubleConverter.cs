using System.Globalization;
using System.Windows.Data;

namespace Synthesis.Core.Tools;

public class HalfConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d) return d / 2.0;
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value  is double d) return 2 * d;
        return 0.0;
    }
}

// 2. NegativeHalfConverter: 把数值除以 -2 (用于修正图片自身的中心点偏移)
public class NegativeHalfConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d) return -d / 2.0;
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return -d * 2.0;
        return 0.0;
    }
}

// 3. InvertDoubleConverter: 把数值乘以 -1 (用于 Y 轴反转)
public class InvertDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d) return -d;
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d) return -d;
        return 0.0;
    }
}
