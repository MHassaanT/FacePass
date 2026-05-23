using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FacePass.Management.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        private static readonly Brush EnrolledBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
        private static readonly Brush PendingBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? EnrolledBrush : PendingBrush;
            return PendingBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
