using System;
using System.Globalization;
using System.Windows.Data;

namespace Launcher.UI.WPF.Helpers
{
    public class PercentToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double scale)
            {
                return scale * 100.0;
            }
            return 100.0;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percent)
            {
                return percent / 100.0;
            }
            return 1.0;
        }
    }
}