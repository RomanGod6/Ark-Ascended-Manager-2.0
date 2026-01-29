using System;
using System.Globalization;
using System.Windows.Data;

namespace Ark_Ascended_Manager.Helpers
{
    public class MapTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOfficial)
            {
                return isOfficial ? "Official" : "Custom";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
