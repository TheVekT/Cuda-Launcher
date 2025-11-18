using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Launcher.UI.WPF.Helpers
{
    public class StringFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is string formatString))
                return DependencyProperty.UnsetValue;
            
            string format = formatString;
            
            object value = values[1] != DependencyProperty.UnsetValue ? values[1] : null;
            
            if (value != null)
            {
                try
                {
                    return string.Format(culture, format, value);
                }
                catch
                {
                    return format;
                }
            }
            
            return format;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}