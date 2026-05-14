using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Launcher.UI.WPF.Views.Instances.Skeletons;

public partial class EditInstanceSkeleton : UserControl
{
    private Storyboard _pulseStoryboard;
    
    public EditInstanceSkeleton()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var opacityAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.4,
            Duration = new Duration(TimeSpan.FromSeconds(0.8)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } 
        };
        
        _pulseStoryboard = new Storyboard();
        _pulseStoryboard.Children.Add(opacityAnimation);
        
        Storyboard.SetTarget(opacityAnimation, SkeletonUI);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
        
        _pulseStoryboard.Begin();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_pulseStoryboard != null)
        {
            _pulseStoryboard.Stop();
            _pulseStoryboard.Children.Clear();
            _pulseStoryboard = null;
        }
    }
}