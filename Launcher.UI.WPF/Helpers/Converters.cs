using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Launcher.UI.WPF.Helpers
{
    public class RadioBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string targetTheme = parameter as string;
            string currentTheme = value as string;
            return string.Equals(currentTheme, targetTheme, StringComparison.InvariantCultureIgnoreCase);
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return parameter;
            }
            return Binding.DoNothing;
        }
    }
    public class ThemeMatchConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            if (values.Length == 2 && values[0] is string currentPath && values[1] is Uri cardUri)
            {
                return string.Equals(currentPath, cardUri.OriginalString, StringComparison.InvariantCultureIgnoreCase);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EdgeAdornerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string side = parameter as string;
            if (side == null) return value;
            
            if (targetType == typeof(CornerRadius) && value is FrameworkElement element)
            {
                var cr = (CornerRadius)element.FindResource("DefaultCorner");
                
                switch (side)
                {
                    case "Bottom": return new CornerRadius(cr.TopLeft, cr.TopRight, 0, 0);
                    case "Top":    return new CornerRadius(0, 0, cr.BottomRight, cr.BottomLeft);
                    default:       return cr;
                }
            }

            if (targetType == typeof(Thickness) && value is Thickness th)
            {
                switch (side)
                {
                    case "Bottom": return new Thickness(th.Left, th.Top, th.Right, 0);
                    case "Top":    return new Thickness(th.Left, 0, th.Right, th.Bottom);
                    default:       return th;
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
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
        public class SnappingWidthConverter : IMultiValueConverter
    {
        private const double CardWidth = 160.0; 
        private const double CardMarginHorizontal = 5.0 + 5.0;
        
        private const double LibraryPadding = 10.0 + 10.0;
        
        private const double ScrollerMargin = 0.0; 
        
        private const double PreviewWidth = 280.0;

        private const double GapBetweenPanels = 10.0; 
        
        private const double TotalPageMargin = 130.0 + 130.0; 
        
        private const double StaticContentWidth = PreviewWidth + GapBetweenPanels + TotalPageMargin;
        
        private const int MinCards = 3;
        private const int MaxCards = 6;

        private static readonly double ItemTotalWidth = Math.Ceiling(CardWidth + CardMarginHorizontal);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (values.Length >= 2 && values[0] is double pageTotalWidth)
            {
                double borderOverhead = 0;
                if (values[1] is Thickness border)
                    borderOverhead = border.Left + border.Right;
                
                double libraryInternalOverhead = borderOverhead + LibraryPadding + ScrollerMargin;
                
                double availableForLibrary = pageTotalWidth - StaticContentWidth;
                
                double usableWidth = availableForLibrary - libraryInternalOverhead;

                if (usableWidth <= 0) 
                    return (MinCards * ItemTotalWidth) + libraryInternalOverhead;
                
                int cardsThatCanFit = (int)Math.Floor((usableWidth + 0.01) / ItemTotalWidth);
                
                if (cardsThatCanFit < MinCards) cardsThatCanFit = MinCards;
                if (cardsThatCanFit > MaxCards) cardsThatCanFit = MaxCards;
                
                double snappedContentWidth = cardsThatCanFit * ItemTotalWidth;
                
                return snappedContentWidth + libraryInternalOverhead;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
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
    
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class AccountTypeToLocConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not AccountType type)
                return string.Empty;

            var loc = System.Windows.Application.Current.Resources["Loc"];
            if (loc == null)
                return string.Empty;

            var indexer = loc.GetType().GetProperty("Item"); 
            if (indexer == null)
                return string.Empty;

            var key = type switch
            {
                AccountType.Microsoft => "AccountCard.MicrosoftType",
                AccountType.Offline   => "AccountCard.OfflineType",
                _                     => null
            };

            return key == null ? string.Empty : indexer.GetValue(loc, new object[] { key });
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}