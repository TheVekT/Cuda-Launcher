using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels;

namespace Launcher.UI.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private AppStore? _appStore;
    
    public MainWindow()
    {
        InitializeComponent();
        
        DataContextChanged += MainWindow_DataContextChanged;
        Loaded += MainWindow_Loaded;
        Unloaded += MainWindow_Unloaded;
        
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
        if (LocalizationService.Instance != null)
        {
            LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
        }
        UpdatePlayButtonState();
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        if (LocalizationService.Instance != null)
        {
            LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
        }
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
            if (LocalizationService.Instance != null)
            {
                PlayButtonCompact.Content = LocalizationService.Instance[isRunning ? LocKey.Play_PlayButton_Close : LocKey.Play_PlayButton];
            }
            PlayButtonCompact.Tag = Application.Current.TryFindResource(isRunning ? "Icon.Close" : "Icon.Play");
        }
    }
}