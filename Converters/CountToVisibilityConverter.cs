using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasyDL.Converters;

public class CountToVisibilityConverter : IValueConverter
{
    public object Translate(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Translate(value, targetType, parameter, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
