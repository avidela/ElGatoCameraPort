using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ElgatoControl.Avalonia.Converters;

public class ChevronConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCollapsed && isCollapsed)
        {
            // Right-pointing chevron (collapsed)
            return "M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z";
        }
        // Down-pointing chevron (expanded)
        return "M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
