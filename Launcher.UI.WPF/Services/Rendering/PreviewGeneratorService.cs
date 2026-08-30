using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Launcher.Core.Config.Abstractions;
using Launcher.UI.WPF.Services.Rendering.Abstractions;
using Microsoft.Web.WebView2.Wpf;

namespace Launcher.UI.WPF.Services.Rendering;

public class PreviewGeneratorService : IPreviewGeneratorService
{
    private readonly string _previewCacheDir;
    private readonly ILauncherPathsService _pathsService;

    public PreviewGeneratorService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        _previewCacheDir = Path.Combine(pathsService.CacheDirectory, "Previews");
        if (!Directory.Exists(_previewCacheDir)) Directory.CreateDirectory(_previewCacheDir);
    }
    
    public string? GenerateCapePreview(string originalCapePath, string capeId)
    {
        if (string.IsNullOrEmpty(originalCapePath) || !File.Exists(originalCapePath)) return null;

        string previewPath = Path.Combine(_previewCacheDir, $"cape_{capeId}.png");
        if (File.Exists(previewPath)) return previewPath; // Already exists, no need to regenerate

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(originalCapePath);
            bitmap.EndInit();
            
            // Cutting front part of the cape (standard Minecraft coordinates)
            var cropped = new CroppedBitmap(bitmap, new Int32Rect(1, 1, 10, 16));
            
            SaveBitmapToFile(cropped, previewPath);
            return previewPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Error generating cape preview: {ex.Message}");
            return null;
        }
    }

    private void SaveBitmapToFile(BitmapSource image, string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create);
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);
    }
    
    public async Task<string?> Generate3DSkinSnapshotAsync(string skinPath, string? capePath, string characterId, bool isSlim, bool forceRegenerate = false)
    {
        if (string.IsNullOrEmpty(skinPath) || !File.Exists(skinPath)) return null;

        string previewPath;

        if (forceRegenerate)
        {
            previewPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}_{DateTime.Now.Ticks}.png");
            
            var oldBasicPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}.png");
            if (File.Exists(oldBasicPath)) {
                try
                {
                    File.Delete(oldBasicPath);
                }
                catch
                {
                    Debug.WriteLine($"[Generator] Warning: Could not delete old basic preview file {oldBasicPath}");
                } 
            }

            var existingFiles = Directory.GetFiles(_previewCacheDir, $"skin3d_{characterId}_*.png");
            foreach (var oldFile in existingFiles) {
                try
                {
                    File.Delete(oldFile);
                }
                catch
                {
                    Debug.WriteLine($"[Generator] Warning: Could not delete old preview file {oldFile}");
                } 
            }
        }
        else
        {
            string oldFormatPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}.png");
            if (File.Exists(oldFormatPath)) return oldFormatPath;

            var existingFiles = Directory.GetFiles(_previewCacheDir, $"skin3d_{characterId}_*.png");
            if (existingFiles.Length > 0) return existingFiles[0];

            previewPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}_{DateTime.Now.Ticks}.png");
        }
        
        // Read skin and cape files and convert to Base64
        string skinBase64 = $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(skinPath))}";
        string? capeBase64 = (string.IsNullOrEmpty(capePath) || !File.Exists(capePath) 
            ? null
            : $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(capePath))}") ?? null;
        
        var webView = new WebView2();
        
        // Create a hidden window to host the WebView2 control
        var hiddenWindow = new Window
        {
            Width = 150, Height = 250,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Opacity = 0,
            ShowInTaskbar = false,
            ShowActivated = false,  
            Left = -9999,             
            Top = -9999,              
            IsHitTestVisible = false,
            Content = webView
        };
        hiddenWindow.Show();

        try
        {
            string browserCachePath = Path.Combine(_pathsService.CacheDirectory, "WebView2Cache");

            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
                null, browserCachePath);
            await webView.EnsureCoreWebView2Async(env);
            
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Web", "skinview.html");
        
            if (!File.Exists(htmlPath))
            {
                Debug.WriteLine($"[Generator] Error: skinview.html not found at {htmlPath}. Please ensure the file exists.");
                return null;
            }
            
            // MUST subscribe BEFORE calling Navigate, otherwise it may hang
            var tcs = new TaskCompletionSource<bool>();
            webView.NavigationCompleted += (_, e) => 
            {
                tcs.TrySetResult(e.IsSuccess);
            };
            
            webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
            
            bool isNavigated = await tcs.Task;
            if (!isNavigated)
            {
                Debug.WriteLine("[Generator] Error: Failed to navigate to the skinview.html page.");
                return null;
            }

            // Wait a bit to ensure the page is fully loaded and ready for script execution
            await Task.Delay(200);

            string jsCapeArg = capeBase64 != null ? $"'{capeBase64}'" : "null";
            string jsLoadCommand = $"loadModel('{skinBase64}', {jsCapeArg}, {(isSlim ? "true" : "false")});";
            await webView.ExecuteScriptAsync(jsLoadCommand);

            // Wait a bit to ensure the model is fully loaded and rendered before taking a snapshot
            await Task.Delay(400);

            string base64Result = await webView.ExecuteScriptAsync("takeSnapshot();");
            base64Result = base64Result.Trim('"'); 
            
            var base64Data = base64Result.Substring(base64Result.IndexOf(',') + 1);
            byte[] imageBytes = Convert.FromBase64String(base64Data);
            
            await File.WriteAllBytesAsync(previewPath, imageBytes);

            return previewPath;
        }
        finally
        {
            webView.Dispose();
            hiddenWindow.Close();
        }
    }
}