using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ark_Ascended_Manager.Helpers
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the server is running (true), return a green brush; otherwise, return a red brush.
            return (bool)value ? Brushes.Green : Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Converting from a Brush back to a boolean is not supported.");
        }
    }
}
