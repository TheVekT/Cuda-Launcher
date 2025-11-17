using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Launcher.UI.WPF.Helpers // Убедитесь, что namespace ваш
{
    public class EdgeAdornerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string side = parameter as string;
            if (side == null) return value;
            
            // --- СЦЕНАРИЙ 1: Конвертируем CornerRadius ---
            // (Сюда прилетит 'value' как FrameworkElement (Border), из которого мы достаем ресурс)
            if (targetType == typeof(CornerRadius) && value is FrameworkElement element)
            {
                // Конвертер сам находит DynamicResource
                var cr = (CornerRadius)element.FindResource("DefaultCorner");
                
                switch (side)
                {
                    case "Bottom": return new CornerRadius(cr.TopLeft, cr.TopRight, 0, 0);
                    case "Top":    return new CornerRadius(0, 0, cr.BottomRight, cr.BottomLeft);
                    // (Можно добавить Left/Right по аналогии)
                    default:       return cr;
                }
            }

            // --- СЦЕНАРИЙ 2: Конвертируем BorderThickness ---
            // (Сюда прилетит 'value' как Thickness, которое мы просто меняем)
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
}