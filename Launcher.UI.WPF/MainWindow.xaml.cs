using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Resources.Pages;
using Launcher.UI.WPF.ViewModels;

namespace Launcher.UI.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<OverlayBlinkMessage>(this, (r, m) =>
        {
            Dispatcher.Invoke(() => PlayBlinkAnimation());
        });
        
        WeakReferenceMessenger.Default.Register<LauncherVisibilityMessage>(this, (r, m) =>
        {
            Dispatcher.Invoke(() => 
            {
                if (m.IsVisible)
                {
                    Show();
                    if (WindowState == WindowState.Minimized)
                        WindowState = WindowState.Normal;
                    
                    Activate();
                }
                else
                    Hide();
            });
        });
    }

    private void PlayBlinkAnimation()
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(120));
        
        var scaleAnimation = new DoubleAnimation(1.0, 1.05, duration) { AutoReverse = true };
        
        var flashAnimation = new DoubleAnimation(0, 0.05, duration) { AutoReverse = true };
        
        OverlayScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        OverlayScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        FlashOverlay.BeginAnimation(UIElement.OpacityProperty, flashAnimation);
    }
}