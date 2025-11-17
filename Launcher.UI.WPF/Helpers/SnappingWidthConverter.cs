using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Launcher.UI.WPF.Helpers
{
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
}