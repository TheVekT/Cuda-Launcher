using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels;

namespace Launcher.UI.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private AppStore? _appStore;
    
    public MainWindow()
    {
        InitializeComponent();
        
        DataContextChanged += MainWindow_DataContextChanged;
        Loaded += MainWindow_Loaded;
        Unloaded += MainWindow_Unloaded;
        StateChanged += MainWindow_StateChanged;
        
        WeakReferenceMessenger.Default.Register<OverlayBlinkMessage>(this, (_, _) =>
        {
            Dispatcher.Invoke(PlayBlinkAnimation);
        });
        
        WeakReferenceMessenger.Default.Register<LauncherVisibilityMessage>(this, (_, m) =>
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

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateLayoutForWindowState();
    }

    private void UpdateLayoutForWindowState()
    {
        if (WindowState == WindowState.Maximized)
        {
            RootGrid.Margin = GetMaximizedMargin();
        }
        else
        {
            RootGrid.Margin = new Thickness(0);
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXFRAME = 32;
    private const int SM_CYFRAME = 33;
    private const int SM_CXPADDEDBORDER = 92;

    private Thickness GetMaximizedMargin()
    {
        int borderX = GetSystemMetrics(SM_CXFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER);
        int borderY = GetSystemMetrics(SM_CYFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER);

        var dpi = VisualTreeHelper.GetDpi(this);
        double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

        return new Thickness(borderX / scaleX, borderY / scaleY, borderX / scaleX, borderY / scaleY);
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
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            
            if (DataContext is MainWindowViewModel vm && vm.DropCommand.CanExecute(files))
                vm.DropCommand.Execute(files);
        }
        e.Handled = true;
    }
    
    private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_appStore != null)
        {
            _appStore.PropertyChanged -= OnAppStorePropertyChanged;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _appStore = vm.AppStore;
            if (_appStore != null)
            {
                _appStore.PropertyChanged += OnAppStorePropertyChanged;
            }
        }
        else
        {
            _appStore = null;
        }

        UpdatePlayButtonState();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
        
        UpdatePlayButtonState();
        UpdateLayoutForWindowState();
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
        StateChanged -= MainWindow_StateChanged;
        
        if (_appStore != null)
        {
            _appStore.PropertyChanged -= OnAppStorePropertyChanged;
        }
    }

    private void OnAppStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppStore.IsGameRunning))
        {
            Dispatcher.Invoke(UpdatePlayButtonState);
        }
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]")
        {
            Dispatcher.Invoke(UpdatePlayButtonState);
        }
    }

    private void UpdatePlayButtonState()
    {
        bool isRunning = _appStore?.IsGameRunning ?? false;

        if (PlayButtonCompact != null)
        {
            PlayButtonCompact.Content = LocalizationService.Instance[isRunning ? LocKey.Play_PlayButton_Close : LocKey.Play_PlayButton];
            PlayButtonCompact.Tag = Application.Current.TryFindResource(isRunning ? "Icon.Close" : "Icon.Play");
        }
    }
}