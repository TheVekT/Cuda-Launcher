using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Launcher.Updater.ViewModels;

namespace Launcher.Updater;

public partial class MainWindow
{
    private IntPtr _nativeBgBrush = IntPtr.Zero;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Unloaded += MainWindow_Unloaded;
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

        return IntPtr.Zero;
    }

    public void UpdateNativeColors()
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

        // Apply dark mode & caption/underlay color matching AppBackground
        DwmSetWindowAttribute(handle, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref useDarkMode, sizeof(int));
        DwmSetWindowAttribute(handle, 19 /* DWMWA_USE_IMMERSIVE_DARK_MODE (older Win10) */, ref useDarkMode, sizeof(int));
        DwmSetWindowAttribute(handle, 35 /* DWMWA_CAPTION_COLOR (Win11) */, ref colorRef, sizeof(int));

        // Fixed rounded corners on Windows 11
        int cornerPref = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(handle, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref cornerPref, sizeof(int));

        // Native GDI background brush for erase
        IntPtr newBrush = CreateSolidBrush(colorRef);
        IntPtr oldBrush = _nativeBgBrush;
        _nativeBgBrush = newBrush;
        if (oldBrush != IntPtr.Zero) DeleteObject(oldBrush);
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_nativeBgBrush != IntPtr.Zero)
        {
            DeleteObject(_nativeBgBrush);
            _nativeBgBrush = IntPtr.Zero;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(int crColor);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDc, [In] ref Rect lprc, IntPtr hbr);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    #endregion
}