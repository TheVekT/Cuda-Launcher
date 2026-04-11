using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;

namespace Launcher.UI.WPF.Resources.Controls;

public partial class InteractiveSkinViewer : UserControl
{
    private bool _isWebViewReady = false;

    public InteractiveSkinViewer()
    {
        InitializeComponent();
        InitializeWebViewAsync();
    }
    
    public static readonly DependencyProperty SkinPathProperty =
        DependencyProperty.Register("SkinPath", typeof(string), typeof(InteractiveSkinViewer), 
            new PropertyMetadata(null, OnModelChanged));

    public string SkinPath
    {
        get => (string)GetValue(SkinPathProperty);
        set => SetValue(SkinPathProperty, value);
    }
    
    public static readonly DependencyProperty CapePathProperty =
        DependencyProperty.Register("CapePath", typeof(string), typeof(InteractiveSkinViewer), 
            new PropertyMetadata(null, OnModelChanged));

    public string CapePath
    {
        get => (string)GetValue(CapePathProperty);
        set => SetValue(CapePathProperty, value);
    }
    
    public static readonly DependencyProperty SkinVariantProperty =
        DependencyProperty.Register("SkinVariant", typeof(string), typeof(InteractiveSkinViewer), 
            new PropertyMetadata("classic", OnModelChanged));

    public string SkinVariant
    {
        get => (string)GetValue(SkinVariantProperty);
        set => SetValue(SkinVariantProperty, value);
    }
    
    public static readonly DependencyProperty AnimationTypeProperty =
        DependencyProperty.Register("AnimationType", typeof(string), typeof(InteractiveSkinViewer), 
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
            viewer.UpdateAnimationInBrowser();
        }
    }
    
    private async void UpdateAnimationInBrowser()
    {
        if (!_isWebViewReady) return;
    
        // Защита от пустых значений
        string animType = string.IsNullOrEmpty(AnimationType) ? "none" : AnimationType.ToLower();
    
        await SkinWebView.ExecuteScriptAsync($"setAnimation('{animType}');");
    }
    
    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InteractiveSkinViewer viewer)
        {
            viewer.UpdateSkinInBrowser();
        }
    }
    
    private async void InitializeWebViewAsync()
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
            
            UpdateSkinInBrowser();
            UpdateAnimationInBrowser();
        };
    }
    
    private async void UpdateSkinInBrowser()
    {
        if (!_isWebViewReady) return;

        try
        {
            // Готовим скин
            string skinBase64 = "null";
            if (!string.IsNullOrEmpty(SkinPath) && File.Exists(SkinPath))
            {
                string base64String = Convert.ToBase64String(File.ReadAllBytes(SkinPath));
                skinBase64 = $"'data:image/png;base64,{base64String}'";
            }

            // Готовим плащ
            string capeBase64 = "null";
            if (!string.IsNullOrEmpty(CapePath) && File.Exists(CapePath))
            {
                string base64String = Convert.ToBase64String(File.ReadAllBytes(CapePath));
                capeBase64 = $"'data:image/png;base64,{base64String}'";
            }

            bool isSlim = SkinVariant?.ToLower() == "slim";
            
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