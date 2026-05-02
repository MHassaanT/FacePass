using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FacePass.Kiosk.Converters
{
    /// <summary>
    /// Converts a bool to WPF Visibility (true → Visible, false → Collapsed).
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility.Visible;
    }
}
