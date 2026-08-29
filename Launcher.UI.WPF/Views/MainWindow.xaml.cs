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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
        UpdateNativeColors();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_ERASEBKGND = 0x0014;
        if (msg == WM_ERASEBKGND && _nativeBgBrush != IntPtr.Zero)
        {
            GetClientRect(hwnd, out RECT rc);
            FillRect(wParam, ref rc, _nativeBgBrush);
            handled = true;
            return (IntPtr)1;
        }

        const int WM_NCHITTEST = 0x0084;
        if (msg == WM_NCHITTEST && WindowState != WindowState.Maximized)
        {
            var result = HandleNcHitTest(hwnd, lParam);
            if (result != IntPtr.Zero)
            {
                handled = true;
                return result;
            }
        }

        return IntPtr.Zero;
    }

    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private IntPtr HandleNcHitTest(IntPtr hwnd, IntPtr lParam)
    {
        long val = lParam.ToInt64();
        int x = (short)(val & 0xFFFF);
        int y = (short)((val >> 16) & 0xFFFF);

        if (!GetWindowRect(hwnd, out RECT rc))
            return IntPtr.Zero;

        var dpi = VisualTreeHelper.GetDpi(this);
        int borderX = (int)Math.Round(6 * (dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0));
        int borderY = (int)Math.Round(6 * (dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0));

        bool isTop = y >= rc.Top && y < rc.Top + borderY;
        bool isBottom = y <= rc.Bottom && y > rc.Bottom - borderY;
        bool isLeft = x >= rc.Left && x < rc.Left + borderX;
        bool isRight = x <= rc.Right && x > rc.Right - borderX;

        if (isTop && isRight) return (IntPtr)HTTOPRIGHT;
        if (isTop && isLeft) return (IntPtr)HTTOPLEFT;
        if (isBottom && isRight) return (IntPtr)HTBOTTOMRIGHT;
        if (isBottom && isLeft) return (IntPtr)HTBOTTOMLEFT;
        if (isTop) return (IntPtr)HTTOP;
        if (isRight) return (IntPtr)HTRIGHT;
        if (isBottom) return (IntPtr)HTBOTTOM;
        if (isLeft) return (IntPtr)HTLEFT;

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

        // Tell DWM to use dark mode frame and caption/resize backdrop matching AppBackground
        int useDarkMode = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) < 128 ? 1 : 0;
        DwmSetWindowAttribute(handle, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref useDarkMode, sizeof(int));
        DwmSetWindowAttribute(handle, 19 /* DWMWA_USE_IMMERSIVE_DARK_MODE (older Win10) */, ref useDarkMode, sizeof(int));
        DwmSetWindowAttribute(handle, 35 /* DWMWA_CAPTION_COLOR (Win11) */, ref colorRef, sizeof(int));

        // Update CornerRadius and DWM Corner Preference based on DefaultCorner
        UpdateCornerRounding(handle);

        // Create Win32 GDI brush for background erase and window class brush
        IntPtr newBrush = CreateSolidBrush(colorRef);
        IntPtr oldBrush = _nativeBgBrush;
        _nativeBgBrush = newBrush;
        SetClassLong(handle, -10 /* GCLP_HBRBACKGROUND */, newBrush);

        if (oldBrush != IntPtr.Zero)
        {
            DeleteObject(oldBrush);
        }
    }

    private void UpdateCornerRounding(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return;

        CornerRadius cornerRadius = new CornerRadius(6);
        if (TryFindResource("DefaultCorner") is CornerRadius cr)
        {
            cornerRadius = cr;
        }

        var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
        if (chrome != null)
        {
            chrome.CornerRadius = cornerRadius;
        }

        // DWMWA_WINDOW_CORNER_PREFERENCE (33 on Windows 11)
        int cornerPref;
        if (WindowState == WindowState.Maximized || cornerRadius.TopLeft == 0)
        {
            cornerPref = 1; // DWMWCP_DONOTROUND
        }
        else if (cornerRadius.TopLeft <= 4)
        {
            cornerPref = 3; // DWMWCP_ROUNDSMALL
        }
        else
        {
            cornerPref = 2; // DWMWCP_ROUND
        }

        DwmSetWindowAttribute(handle, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref cornerPref, sizeof(int));
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateLayoutForWindowState();
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            UpdateCornerRounding(handle);
        }
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

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(int crColor);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
    private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetClassLong")]
    private static extern int SetClassLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr SetClassLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8 
            ? SetClassLongPtr64(hWnd, nIndex, dwNewLong) 
            : new IntPtr(SetClassLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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