using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows;
using Launcher.UI.WPF.Services;
using Launcher.Core.Services.System;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Helpers;

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
public class EnumToBooleanConverter : IValueConverter
{
    // Из ViewModel в XAML (Проверяем, совпадает ли значение с параметром)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        
        string checkValue = value.ToString();
        string targetValue = parameter.ToString();
        
        return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
    }

    // Из XAML в ViewModel (Если радиокнопка стала True, возвращаем Enum)
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Binding.DoNothing;
        
        bool useValue = (bool)value;
        string targetValue = parameter.ToString();
        
        if (useValue)
        {
            // Конвертируем строку-параметр обратно в Enum
            return Enum.Parse(targetType, targetValue); 
        }

        return Binding.DoNothing;
    }
}

public class TimeAgoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 1. Обработка null (Если игры никогда не запускались)
        if (value == null)
            return LocalizationService.Instance["VersionElement.LastPlayed.Never"]; // Ты писал "Newer", но по смыслу LastPlayed это "Never" (Никогда)

        // 2. Проверка типа
        if (value is not DateTime date)
            return LocalizationService.Instance["VersionElement.LastPlayed.Never"];

        // Если дата "минимальная" (дефолтная), считаем что не играли
        if (date == DateTime.MinValue)
            return LocalizationService.Instance["VersionElement.LastPlayed.Never"];

        var timeSpan = DateTime.Now - date;

        // 3. Логика "Сколько времени прошло"
        
        // Меньше минуты
        if (timeSpan.TotalSeconds < 60)
            return LocalizationService.Instance["VersionElement.LastPlayed.JustNow"];

        // Меньше часа (минуты)
        if (timeSpan.TotalMinutes < 60)
            return string.Format(LocalizationService.Instance["VersionElement.LastPlayed.XMinsAgo"], timeSpan.Minutes);

        // Меньше суток (часы)
        if (timeSpan.TotalHours < 24)
        {
            // Можно добавить логику для "1 hr." vs "2 hrs.", но обычно сокращения hr. достаточно
            return string.Format(LocalizationService.Instance["VersionElement.LastPlayed.XHoursAgo"], timeSpan.Hours);
        }

        // Меньше 48 часов (вчера / 1 день назад)
        if (timeSpan.TotalDays < 2)
            return LocalizationService.Instance["VersionElement.LastPlayed.1DayAgo"];

        // Меньше месяца (дни)
        if (timeSpan.TotalDays < 30)
            return string.Format(LocalizationService.Instance["VersionElement.LastPlayed.XDaysAgo"], timeSpan.Days);

        // Меньше года (месяцы)
        if (timeSpan.TotalDays < 365)
        {
            int months = (int)(timeSpan.TotalDays / 30);
            if (months <= 1)
            {
                return LocalizationService.Instance["VersionElement.LastPlayed.1MonthAgo"];
            }
            else
            {
                return string.Format(LocalizationService.Instance["VersionElement.LastPlayed.XMonthsAgo"], months);
            }
        }

        // Больше года
        // Если чуть больше года
        if (timeSpan.TotalDays < 730) // меньше 2 лет
            return LocalizationService.Instance["VersionElement.LastPlayed.OverYearAgo"];
        
        // Если много лет
        int years = (int)(timeSpan.TotalDays / 365);
        return string.Format(LocalizationService.Instance["VersionElement.LastPlayed.XYearsAgo"], years);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ConfirmButtonVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConfirmButtons currentMask && parameter is ConfirmButtons targetFlag)
        {
            return (currentMask & targetFlag) == targetFlag 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EqualConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        // Сравниваем значение из Binding с параметром из XAML
        return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}

public class ImagePathConverter : IValueConverter
{
    private readonly ILauncherPathsService _pathsService;

    public ImagePathConverter()
    {
        _pathsService = App.Services.GetRequiredService<ILauncherPathsService>();
    }
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string fileName && !string.IsNullOrEmpty(fileName))
        {
            if (Path.IsPathRooted(fileName)) return fileName;

            return Path.Combine(Path.Combine(_pathsService.AssetsDirectory, "Icons"), fileName);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class NotEqualConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return true;
        // Возвращаем true, если значения НЕ совпадают
        return !value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}
