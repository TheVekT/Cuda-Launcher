using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Services;

public class ThemeService
{
    private readonly string _themesRoot;  // Рабочая папка: Assets/Themes (рядом с exe)
    private readonly string _cacheRoot;   // Кэш: Assets/Themes/Cache
    private readonly ILauncherPathsService _pathsService;
    private List<ThemeModel> _availableThemes = new List<ThemeModel>();
    private ThemeModel _currentTheme;
    
    public ThemeModel CurrentTheme => _currentTheme;
    
    // Имена файлов, которые мы ищем ВНУТРИ ресурсов и создаем НА ДИСКЕ
    private readonly string[] _defaultThemeFiles = { "default-dark.zip", "default-light.zip" };

    public ThemeService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        // Настраиваем пути
        _themesRoot = Path.Combine(_pathsService.AssetsDirectory, "Themes");
        _cacheRoot = Path.Combine(_themesRoot, "Cache");

        Directory.CreateDirectory(_themesRoot);
        Directory.CreateDirectory(_cacheRoot);
    }
    
    public ThemeModel GetThemeByFileName(string fileName)
    {
        return _availableThemes.FirstOrDefault(t => t.ZipPath.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }
    
    public async Task ImportTheme(string themeFilePath)
    {
        if (string.IsNullOrEmpty(themeFilePath) || !File.Exists(themeFilePath)) return;

        var destPath = Path.Combine(_themesRoot, Path.GetFileName(themeFilePath));
        File.Copy(themeFilePath, destPath, true);
        WeakReferenceMessenger.Default.Send(new ThemeImportedMessage());
    }

    // === 2. Загрузка списка тем ===
    public List<ThemeModel> ReloadThemes()
    {
        var list = new List<ThemeModel>();
        if (!Directory.Exists(_themesRoot)) return list;

        // Чистим кэш перед сканированием
        try 
        {
            if (Directory.Exists(_cacheRoot)) 
            {
                Directory.Delete(_cacheRoot, true);
                Directory.CreateDirectory(_cacheRoot);
            }
        }
        catch { }

        var zipFiles = Directory.GetFiles(_themesRoot, "*.zip");

        foreach (var zipPath in zipFiles)
        {
            try
            {
                var model = ExtractAndProcessTheme(zipPath);
                if (model != null) list.Add(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme {zipPath}: {ex.Message}");
            }
        }
        
        var res = list.OrderBy(t => 
            {
                var fName = Path.GetFileName(t.ZipPath);
                int index = Array.IndexOf(_defaultThemeFiles, fName);
                return index >= 0 ? index : int.MaxValue;
            })
            .ThenBy(t => t.Name)
            .ToList();
        _availableThemes = res;
        return res;
    }

    // === 3. Распаковка и обработка одной темы ===
    private ThemeModel ExtractAndProcessTheme(string zipPath)
    {
        var folderName = Path.GetFileNameWithoutExtension(zipPath);
        var themeCacheDir = Path.Combine(_cacheRoot, folderName);
        Directory.CreateDirectory(themeCacheDir);

        // Распаковка (перезаписываем, если есть)
        ZipFile.ExtractToDirectory(zipPath, themeCacheDir, true);

        var themeXamlPath = Path.Combine(themeCacheDir, "Theme.xaml");
        if (!File.Exists(themeXamlPath)) return null;

        // Читаем XAML
        string xamlContent = File.ReadAllText(themeXamlPath);

        // Патчим пути к шрифтам
        var baseUri = new Uri(themeCacheDir + Path.DirectorySeparatorChar).AbsoluteUri;
        xamlContent = Regex.Replace(xamlContent, @"[""']?[\\/]Fonts[\\/]", match => $"{baseUri}Fonts/");

        // Патчим Namespace (ThemeMetaData)
        xamlContent = PatchXamlNamespace(xamlContent);

        // Перезаписываем файл
        File.WriteAllText(themeXamlPath, xamlContent);

        // Парсим имя и автора
        var nameMatch = Regex.Match(xamlContent, @"Name=""([^""]*)""");
        var authorMatch = Regex.Match(xamlContent, @"Author=""([^""]*)""");

        var model = new ThemeModel
        {
            // ИЗМЕНЕНИЕ: Берем только имя файла (например "default-light.zip")
            ZipPath = Path.GetFileName(zipPath), 
            XamlPath = themeXamlPath,
            Name = nameMatch.Success ? nameMatch.Groups[1].Value : folderName,
            Author = authorMatch.Success ? authorMatch.Groups[1].Value : "Unknown",
            BannerPath = null 
        };

        // Ищем баннер (в корне папки темы или в папке Banner)
        // Ищем файлы .png или .jpg (без учета регистра)
        var possibleDirs = new[] { themeCacheDir, Path.Combine(themeCacheDir, "Banner") };
        
        foreach (var dir in possibleDirs)
        {
            if (Directory.Exists(dir))
            {
                var file = Directory.GetFiles(dir)
                    .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                         f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
                
                if (file != null)
                {
                    model.BannerPath = file;
                    break; // Нашли - выходим
                }
            }
        }

        return model;
    }

    // === 4. Применение темы ===
    public void ChangeTheme(string themeFileName) // Переименовал параметр для понятности
    {
        if (string.IsNullOrEmpty(themeFileName)) return;

        // ИЗМЕНЕНИЕ: Динамически собираем абсолютный путь к архиву
        string zipPath = Path.Combine(_themesRoot, themeFileName);

        if (!File.Exists(zipPath)) return;

        try
        {
            var folderName = Path.GetFileNameWithoutExtension(zipPath);
            var themeCacheDir = Path.Combine(_cacheRoot, folderName);
            var themeXamlPath = Path.Combine(themeCacheDir, "Theme.xaml");

            // Если кэша нет - распаковываем
            if (!File.Exists(themeXamlPath))
            {
                ExtractAndProcessTheme(zipPath);
            }

            if (!File.Exists(themeXamlPath)) return;

            // Читаем и еще раз проверяем патч неймспейса (на всякий случай)
            string xamlContent = File.ReadAllText(themeXamlPath);
            xamlContent = PatchXamlNamespace(xamlContent);

            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xamlContent)))
            {
                var parserContext = new ParserContext();
                parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                
                string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                parserContext.XmlnsDictionary.Add("metadata", $"clr-namespace:Launcher.UI.WPF.Models;assembly={assemblyName}");

                var newDict = (ResourceDictionary)XamlReader.Load(stream, parserContext);
                ReplaceApplicationResources(newDict);
            }
            _currentTheme = GetThemeByFileName(themeFileName);
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(themeFileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to apply theme: {ex.Message}");
        }
    }

    // === Вспомогательные методы ===

    private string PatchXamlNamespace(string xamlContent)
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        string oldNs = "clr-namespace:Launcher.UI.WPF.Models";
        string newNs = $"clr-namespace:Launcher.UI.WPF.Models;assembly={assemblyName}";

        if (xamlContent.Contains(oldNs) && !xamlContent.Contains(oldNs + ";assembly="))
        {
            return xamlContent.Replace(oldNs, newNs);
        }
        return xamlContent;
    }

    private void ReplaceApplicationResources(ResourceDictionary newDict)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var oldTheme = dicts.FirstOrDefault(d => d.Contains("ThemeInfo"));
        var oldBrushes = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Brushes.xaml"));
        
        if (oldTheme != null)
            dicts[dicts.IndexOf(oldTheme)] = newDict;
        else
            dicts.Add(newDict);
        if (oldBrushes != null)
            dicts[dicts.IndexOf(oldBrushes)] = new ResourceDictionary { Source = new Uri("Resources/Styles/Brushes.xaml", UriKind.Relative) };
    }
}
