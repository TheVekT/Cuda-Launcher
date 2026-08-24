using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Launcher.Core.Config.Abstractions;
using Launcher.UI.WPF.Services.Abstractions;
using Microsoft.Web.WebView2.Wpf;

namespace Launcher.UI.WPF.Services;

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
    
    public string GenerateCapePreview(string originalCapePath, string capeId)
    {
        if (string.IsNullOrEmpty(originalCapePath) || !File.Exists(originalCapePath)) return null;

        string previewPath = Path.Combine(_previewCacheDir, $"cape_{capeId}.png");
        if (File.Exists(previewPath)) return previewPath; // Уже есть в кэше

        try
        {
            // Загружаем оригинальную текстуру (например 64x32)
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(originalCapePath);
            bitmap.EndInit();

            // Вырезаем лицевую часть плаща (стандартные координаты Minecraft)
            var cropped = new CroppedBitmap(bitmap, new Int32Rect(1, 1, 10, 16));

            // Сохраняем в файл
            SaveBitmapToFile(cropped, previewPath);
            return previewPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка генерации превью плаща: {ex.Message}");
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
    
    public async Task<string> Generate3DSkinSnapshotAsync(string skinPath, string? capePath, string characterId, bool isSlim, bool forceRegenerate = false)
    {
        if (string.IsNullOrEmpty(skinPath) || !File.Exists(skinPath)) return null;

        string previewPath;

        if (forceRegenerate)
        {
            previewPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}_{DateTime.Now.Ticks}.png");
            
            string oldBasicPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}.png");
            if (File.Exists(oldBasicPath)) { try { File.Delete(oldBasicPath); } catch { } }

            var existingFiles = Directory.GetFiles(_previewCacheDir, $"skin3d_{characterId}_*.png");
            foreach (var oldFile in existingFiles) { try { File.Delete(oldFile); } catch { } }
        }
        else
        {
            string oldFormatPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}.png");
            if (File.Exists(oldFormatPath)) return oldFormatPath;

            var existingFiles = Directory.GetFiles(_previewCacheDir, $"skin3d_{characterId}_*.png");
            if (existingFiles.Length > 0) return existingFiles[0];

            previewPath = Path.Combine(_previewCacheDir, $"skin3d_{characterId}_{DateTime.Now.Ticks}.png");
        }

        // 1. Читаем файлы скина и плаща и превращаем в Base64
        string skinBase64 = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(skinPath))}";string capeBase64 = string.IsNullOrEmpty(capePath) || !File.Exists(capePath) 
            ? null 
            : $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(capePath))}";

        // 2. Создаем WebView2
        var webView = new WebView2();
        
        // БРОНЯ ДЛЯ WPF: Создаем прозрачное невидимое окно, чтобы WebView2 получил HWND и смог рендерить WebGL
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

            // 3. ГРУЗИМ HTML ИЗ ПРАВИЛЬНОЙ ПАПКИ (Assets/Web/)
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Web", "skinview.html");
        
            if (!File.Exists(htmlPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Generator] ОШИБКА: Файл {htmlPath} не найден!");
                return null;
            }

            // ОБЯЗАТЕЛЬНО: подписываемся ДО вызова Navigate, чтобы не зависнуть
            var tcs = new TaskCompletionSource<bool>();
            webView.NavigationCompleted += (s, e) => 
            {
                tcs.TrySetResult(e.IsSuccess);
            };
            
            webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
            
            bool isNavigated = await tcs.Task;
            if (!isNavigated)
            {
                System.Diagnostics.Debug.WriteLine("[Generator] ОШИБКА: WebView не смог загрузить HTML.");
                return null;
            }

            // Даем JS время на загрузку Three.js
            await Task.Delay(200);

            string jsCapeArg = capeBase64 != null ? $"'{capeBase64}'" : "null";
            string jsLoadCommand = $"loadModel('{skinBase64}', {jsCapeArg}, {(isSlim ? "true" : "false")});";
            await webView.ExecuteScriptAsync(jsLoadCommand);

            // Ждем пока текстуры натянутся на 3D модель
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