using System;
using System.Globalization;
using System.Windows.Data;

namespace FacePass.Management.Converters
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "Enrolled" : "Pending";
            return "Pending";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
