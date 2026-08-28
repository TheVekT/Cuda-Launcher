using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows;
using Launcher.Core.Config.Abstractions;
using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Services.Customization;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Helpers;

public class EdgeAdornerConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string side) return value;
        
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

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PercentToScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double scale)
        {
            return scale * 100.0;
        }
        return 100.0;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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
        
        var value = values[1] != DependencyProperty.UnsetValue ? values[1] : null;

        if (value == null) return format;
        try
        {
            return string.Format(culture, format, value);
        }
        catch
        {
            return format;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NotNullToVisibilityConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;

        if (targetType == typeof(Visibility))
        {
            if (isNotNull)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        return isNotNull;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class TypeMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.GetType().Name == parameter.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        
        var checkValue = value.ToString();
        var targetValue = parameter.ToString();
        
        return checkValue?.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Binding.DoNothing;
        
        var useValue = (bool)value;
        var targetValue = parameter.ToString()!;
        
        if (useValue)
            return Enum.Parse(targetType, targetValue); 
        
        return Binding.DoNothing;
    }
}

public class TimeAgoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || value is not DateTime date || date == DateTime.MinValue)
            return LocalizationService.Instance[LocKey.VersionElement_LastPlayed_Never];

        var timeSpan = DateTime.Now - date;
        
        // less than a minute
        if (timeSpan.TotalSeconds < 60)
            return LocalizationService.Instance[LocKey.VersionElement_LastPlayed_JustNow];

        // less than an hour (minutes)
        if (timeSpan.TotalMinutes < 60)
            return string.Format(LocalizationService.Instance[LocKey.VersionElement_LastPlayed_XMinsAgo], timeSpan.Minutes);

        // less than a day (hours)
        if (timeSpan.TotalHours < 24)
        {
            return string.Format(LocalizationService.Instance[LocKey.VersionElement_LastPlayed_XHoursAgo], timeSpan.Hours);
        }

        switch (timeSpan.TotalDays)
        {
            // less than 48 hours (yesterday / 1 day ago)
            case < 2:
                return LocalizationService.Instance[LocKey.VersionElement_LastPlayed_1DayAgo];
            // less than a month (days)
            case < 30:
                return string.Format(LocalizationService.Instance[LocKey.VersionElement_LastPlayed_XDaysAgo], timeSpan.Days);
            // less than a year (months)
            case < 365:
            {
                var months = (int)(timeSpan.TotalDays / 30);
                return months <= 1 ? LocalizationService.Instance[LocKey.VersionElement_LastPlayed_1MonthAgo] 
                    : string.Format(LocalizationService.Instance[LocKey.VersionElement_LastPlayed_XMonthsAgo], months);
            }
            // More than a year
            // If just over a year
            // less than 2 years
            case < 730:
                return LocalizationService.Instance[LocKey.VersionElement_LastPlayed_OverYearAgo];
            default:
            {
                // if more than 2 years, show the number of years
                var years = (int)(timeSpan.TotalDays / 365);
                return string.Format(LocalizationService.Instance[LocKey.VersionElement_LastPlayed_XYearsAgo], years);
            }
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumFlagToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum currentMask && parameter is Enum targetFlag)
        {
            return currentMask.HasFlag(targetFlag) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EqualConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Invert;
        bool equals = value.ToString()!.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return Invert ? !equals : equals;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}

public class ImagePathConverter : IValueConverter
{
    private readonly ILauncherPathsService _pathsService = App.Services!.GetRequiredService<ILauncherPathsService>();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var iconsDir = Path.Combine(_pathsService.AssetsDirectory, "Icons");
        var defaultIconPath = Path.Combine(iconsDir, "logo.png");

        if (value is string fileName && !string.IsNullOrEmpty(fileName))
        {
            string fullPath = Path.IsPathRooted(fileName) 
                ? fileName 
                : Path.Combine(iconsDir, fileName);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        
        return File.Exists(defaultIconPath) ? defaultIconPath : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}

