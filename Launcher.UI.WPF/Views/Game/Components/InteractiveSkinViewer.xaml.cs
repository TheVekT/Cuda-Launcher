using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;

namespace Launcher.UI.WPF.Views.Game.Components;

public partial class InteractiveSkinViewer
{
    private bool _isWebViewReady;

    public InteractiveSkinViewer()
    {
        InitializeComponent();
        _ = InitializeWebViewAsync();
    }
    
    public static readonly DependencyProperty SkinPathProperty =
        DependencyProperty.Register(nameof(SkinPath), typeof(string), typeof(InteractiveSkinViewer), 
            new PropertyMetadata(null, OnModelChanged));

    public string SkinPath
    {
        get => (string)GetValue(SkinPathProperty);
        set => SetValue(SkinPathProperty, value);
    }
    
    public static readonly DependencyProperty CapePathProperty =
        DependencyProperty.Register(nameof(CapePath), typeof(string), typeof(InteractiveSkinViewer), 
            new PropertyMetadata(null, OnModelChanged));

    public string CapePath
    {
        get => (string)GetValue(CapePathProperty);
        set => SetValue(CapePathProperty, value);
    }
    
    public static readonly DependencyProperty SkinVariantProperty =
        DependencyProperty.Register(nameof(SkinVariant), typeof(string), typeof(InteractiveSkinViewer), 
            new PropertyMetadata("classic", OnModelChanged));

    public string SkinVariant
    {
        get => (string)GetValue(SkinVariantProperty);
        set => SetValue(SkinVariantProperty, value);
    }
    
    public static readonly DependencyProperty AnimationTypeProperty =
        DependencyProperty.Register(nameof(AnimationType), typeof(string), typeof(InteractiveSkinViewer), 
            new PropertyMetadata("idle", OnAnimationChanged));

    public string AnimationType
    {
        get => (string)GetValue(AnimationTypeProperty);
        set => SetValue(AnimationTypeProperty, value);
    }
    
    private static void OnAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InteractiveSkinViewer viewer && viewer._isWebViewReady)
        {
            _ = viewer.UpdateAnimationInBrowser();
        }
    }
    
    private async Task UpdateAnimationInBrowser()
    {
        if (!_isWebViewReady) return;
        
        string animType = string.IsNullOrEmpty(AnimationType) ? "none" : AnimationType.ToLower();
    
        await SkinWebView.ExecuteScriptAsync($"setAnimation('{animType}');");
    }
    
    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InteractiveSkinViewer viewer)
        {
            _ = viewer.UpdateSkinInBrowser();
        }
    }
    
    private async Task InitializeWebViewAsync()
    {
        string browserCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "WebView2Cache");
        var env = await CoreWebView2Environment.CreateAsync(null, browserCachePath);
        await SkinWebView.EnsureCoreWebView2Async(env);

      
        SkinWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        SkinWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        SkinWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        SkinWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        
        string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Web","skinview.html");
        SkinWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);

        SkinWebView.NavigationCompleted += (s, e) =>
        {
            _isWebViewReady = true;
            
            _ = UpdateSkinInBrowser();
            _ = UpdateAnimationInBrowser();
        };
    }
    
    private async Task UpdateSkinInBrowser()
    {
        if (!_isWebViewReady) return;

        try
        {
            string skinBase64 = "null";
            if (!string.IsNullOrEmpty(SkinPath) && File.Exists(SkinPath))
            {
                string base64String = Convert.ToBase64String(await File.ReadAllBytesAsync(SkinPath));
                skinBase64 = $"'data:image/png;base64,{base64String}'";
            }
            
            string capeBase64 = "null";
            if (!string.IsNullOrEmpty(CapePath) && File.Exists(CapePath))
            {
                string base64String = Convert.ToBase64String(await File.ReadAllBytesAsync(CapePath));
                capeBase64 = $"'data:image/png;base64,{base64String}'";
            }

            bool isSlim = SkinVariant.Equals("slim", StringComparison.CurrentCultureIgnoreCase);
            
            string jsCommand = $"loadModel({skinBase64}, {capeBase64}, {(isSlim ? "true" : "false")});";
            await SkinWebView.ExecuteScriptAsync(jsCommand);
            await Task.Delay(100);


            if (SkinWebView.Opacity < 1)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.2)
                };
                SkinWebView.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InteractiveSkinViewer] Ошибка обновления: {ex.Message}");
        }
    }
}