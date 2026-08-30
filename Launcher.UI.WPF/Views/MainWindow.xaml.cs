using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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
    private IntPtr _nativeBgBrush = IntPtr.Zero;
    
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

        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (_, _) =>
        {
            Dispatcher.Invoke(UpdateNativeColors);
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

    #region Native Window & Theme Integration

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
        UpdateNativeColors();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0014 && _nativeBgBrush != IntPtr.Zero) // WM_ERASEBKGND
        {
            GetClientRect(hwnd, out Rect rc);
            FillRect(wParam, ref rc, _nativeBgBrush);
            handled = true;
            return 1;
        }

        if (msg == 0x0084 && WindowState != WindowState.Maximized) // WM_NCHITTEST
        {
            var hit = HandleNcHitTest(hwnd, lParam);
            if (hit != IntPtr.Zero)
            {
                handled = true;
                return hit;
            }
        }

        return IntPtr.Zero;
    }

    private IntPtr HandleNcHitTest(IntPtr hwnd, IntPtr lParam)
    {
        long val = lParam.ToInt64();
        int x = (short)(val & 0xFFFF);
        int y = (short)((val >> 16) & 0xFFFF);

        if (!GetWindowRect(hwnd, out Rect rc)) return IntPtr.Zero;

        var dpi = VisualTreeHelper.GetDpi(this);
        int bx = (int)Math.Round(6 * (dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0));
        int by = (int)Math.Round(6 * (dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0));

        bool top = y < rc.Top + by, bottom = y > rc.Bottom - by;
        bool left = x < rc.Left + bx, right = x > rc.Right - bx;

        if (top && right) return 14; // HTTOPRIGHT
        if (top && left) return 13;  // HTTOPLEFT
        if (bottom && right) return 17; // HTBOTTOMRIGHT
        if (bottom && left) return 16;  // HTBOTTOMLEFT
        if (top) return 12; // HTTOP
        if (right) return 11; // HTRIGHT
        if (bottom) return 15; // HTBOTTOM
        if (left) return 10; // HTLEFT

        return IntPtr.Zero;
    }

    private void UpdateNativeColors()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;

        Color color = Colors.Black;
        if (TryFindResource("AppBackgroundBrush") is SolidColorBrush scb)
            color = scb.Color;
        else if (TryFindResource("AppBackground") is Color c)
            color = c;

        int colorRef = color.R | (color.G << 8) | (color.B << 16);
        int useDarkMode = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) < 128 ? 1 : 0;

        DwmSetWindowAttribute(handle, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref useDarkMode, sizeof(int));
        DwmSetWindowAttribute(handle, 19 /* DWMWA_USE_IMMERSIVE_DARK_MODE (older Win10) */, ref useDarkMode, sizeof(int));
        DwmSetWindowAttribute(handle, 35 /* DWMWA_CAPTION_COLOR (Win11) */, ref colorRef, sizeof(int));

        UpdateCornerRounding(handle);

        IntPtr newBrush = CreateSolidBrush(colorRef);
        IntPtr oldBrush = _nativeBgBrush;
        _nativeBgBrush = newBrush;
        if (oldBrush != IntPtr.Zero) DeleteObject(oldBrush);
    }

    private void UpdateCornerRounding(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return;

        var corner = TryFindResource("DefaultCorner") as CornerRadius? ?? new CornerRadius(6);
        var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
        if (chrome != null) chrome.CornerRadius = corner;

        // DWMWA_WINDOW_CORNER_PREFERENCE (Win11): 1 = DONOTROUND, 2 = ROUND, 3 = ROUNDSMALL
        int cornerPref = (WindowState == WindowState.Maximized || corner.TopLeft == 0) ? 1
                       : corner.TopLeft <= 4 ? 3 : 2;

        DwmSetWindowAttribute(handle, 33, ref cornerPref, sizeof(int));
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        RootGrid.Margin = WindowState == WindowState.Maximized ? GetMaximizedMargin() : new Thickness(0);
        UpdateCornerRounding(new WindowInteropHelper(this).Handle);
    }

    private Thickness GetMaximizedMargin()
    {
        int borderX = GetSystemMetrics(32 /* SM_CXFRAME */) + GetSystemMetrics(92 /* SM_CXPADDEDBORDER */);
        int borderY = GetSystemMetrics(33 /* SM_CYFRAME */) + GetSystemMetrics(92 /* SM_CXPADDEDBORDER */);

        var dpi = VisualTreeHelper.GetDpi(this);
        double sx = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        double sy = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

        return new Thickness(borderX / sx, borderY / sy, borderX / sx, borderY / sy);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(int crColor);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDс, [In] ref Rect lprc, IntPtr hbr);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    #endregion

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
        RootGrid.Margin = WindowState == WindowState.Maximized ? GetMaximizedMargin() : new Thickness(0);
        UpdateNativeColors();
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
        StateChanged -= MainWindow_StateChanged;

        if (_nativeBgBrush != IntPtr.Zero)
        {
            DeleteObject(_nativeBgBrush);
            _nativeBgBrush = IntPtr.Zero;
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
            PlayButtonCompact.Content = LocalizationService.Instance[isRunning ? LocKey.Play_PlayButton_Close : LocKey.Play_PlayButton];
            PlayButtonCompact.Tag = Application.Current.TryFindResource(isRunning ? "Icon.Close" : "Icon.Play");
        }
    }
}