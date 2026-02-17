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
using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Services
{
    public class ThemeService
    {
        private readonly string _themesRoot;  // Рабочая папка: Assets/Themes (рядом с exe)
        private readonly string _cacheRoot;   // Кэш: Assets/Themes/Cache

        // Имена файлов, которые мы ищем ВНУТРИ ресурсов и создаем НА ДИСКЕ
        private readonly string[] _defaultThemeFiles = { "default-dark.zip", "default-light.zip" };

        public ThemeService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Настраиваем пути
            _themesRoot = Path.Combine(baseDir, "Assets", "Themes");
            _cacheRoot = Path.Combine(_themesRoot, "Cache");

            Directory.CreateDirectory(_themesRoot);
            Directory.CreateDirectory(_cacheRoot);

            // 1. Восстанавливаем дефолтные темы из Embedded Resources
            RestoreEmbeddedThemes();
        }

        private void RestoreEmbeddedThemes()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var allResources = assembly.GetManifestResourceNames();

                foreach (var fileName in _defaultThemeFiles)
                {
                    // Путь, куда файл должен лечь физически (Assets/Themes/...)
                    var destPath = Path.Combine(_themesRoot, fileName);

                    // Если файла нет или он 0 байт — восстанавливаем
                    if (!File.Exists(destPath) || new FileInfo(destPath).Length == 0)
                    {
                        // Ищем ресурс по окончанию имени. 
                        // Visual Studio обычно называет их: Launcher.UI.WPF.Resources.Embedded.Themes.default-dark.zip
                        // EndsWith найдет его независимо от namespace.
                        var resourceName = allResources.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrEmpty(resourceName))
                        {
                            using (var stream = assembly.GetManifestResourceStream(resourceName))
                            using (var fileStream = File.Create(destPath))
                            {
                                stream?.CopyTo(fileStream);
                            }
                            // Лог для проверки (можно убрать)
                            System.Diagnostics.Debug.WriteLine($"[ThemeService] Restored embedded theme: {fileName}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ThemeService] CRITICAL: Resource ending with '{fileName}' not found in assembly!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeService] RestoreEmbeddedThemes Error: {ex.Message}");
            }
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

            // Сортировка: Сначала дефолтные (в порядке массива), потом остальные
            return list.OrderBy(t => 
            {
                var fName = Path.GetFileName(t.ZipPath);
                int index = Array.IndexOf(_defaultThemeFiles, fName);
                return index >= 0 ? index : int.MaxValue;
            })
            .ThenBy(t => t.Name)
            .ToList();
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
                ZipPath = zipPath,
                XamlPath = themeXamlPath,
                Name = nameMatch.Success ? nameMatch.Groups[1].Value : folderName,
                Author = authorMatch.Success ? authorMatch.Groups[1].Value : "Unknown",
                // По умолчанию null
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
        public void ChangeTheme(string zipPath)
        {
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath)) return;

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
            if (oldTheme != null) dicts.Remove(oldTheme);

            var brushes = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Brushes.xaml"));
            if (brushes != null) dicts.Remove(brushes);

            dicts.Add(newDict);
            dicts.Add(new ResourceDictionary { Source = new Uri("Resources/Styles/Brushes.xaml", UriKind.Relative) });
        }
    }
}