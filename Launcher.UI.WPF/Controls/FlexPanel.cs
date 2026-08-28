using System.Windows;
using System.Windows.Controls;

namespace Launcher.UI.WPF.Controls;

public class FlexPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double totalDesiredWidth = 0;
        double maxHeight = 0;

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            
            totalDesiredWidth += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }
        
        double finalWidth = double.IsInfinity(availableSize.Width) 
            ? totalDesiredWidth 
            : availableSize.Width;
        
        double finalHeight = double.IsInfinity(availableSize.Height) 
            ? maxHeight 
            : availableSize.Height;
        
        if (maxHeight == 0 && finalHeight == 0) finalHeight = 30; 

        return new Size(finalWidth, finalHeight);
    }
    
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0) return finalSize;
        
        double totalDesiredWidth = 0;
        foreach (UIElement child in InternalChildren)
        {
            totalDesiredWidth += child.DesiredSize.Width;
        }
        
        double extraSpace = finalSize.Width - totalDesiredWidth;
        double extraPerItem = extraSpace / InternalChildren.Count;

        double currentX = 0;

        foreach (UIElement child in InternalChildren)
        {
            double itemWidth = Math.Max(0, child.DesiredSize.Width + extraPerItem);
            double itemHeight = Math.Max(0, finalSize.Height);

            child.Arrange(new Rect(currentX, 0, itemWidth, itemHeight));
            currentX += itemWidth;
        }

        return finalSize;
    }
}
