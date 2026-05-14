using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.ViewModels;

namespace Launcher.UI.WPF.Views;

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
    
    private void UIElement_OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            if (DataContext is MainWindowViewModel vm && vm.DragEnterCommand.CanExecute(null))
                vm.DragEnterCommand.Execute(null);
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void UIElement_OnDragLeave(object sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.DragLeaveCommand.CanExecute(null))
            vm.DragLeaveCommand.Execute(null);
        e.Handled = true;
    }

    private void UIElement_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            
            if (DataContext is MainWindowViewModel vm && vm.DropCommand.CanExecute(files))
                vm.DropCommand.Execute(files);
        }
        e.Handled = true;
    }
}