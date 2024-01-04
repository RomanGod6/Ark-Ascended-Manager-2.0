using System;
using System.Globalization;
using System.Windows.Data;
using System.Collections.Generic;
using System.Linq;

namespace Ark_Ascended_Manager.Helpers
{
    internal class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a List<string>
            if (value is List<string> list)
            {
                // Join the list into a single string, separated by commas
                return string.Join(", ", list);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not implemented as it's not needed for one-way binding
            throw new NotImplementedException();
        }
    }
}
