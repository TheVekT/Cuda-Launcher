using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Customization.Abstractions;
using Launcher.Infrastructure.Customization.Models;

namespace Launcher.Infrastructure.Customization;

public class ThemeProvider : IThemeProvider
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILauncherPathsService _pathsService;
    private readonly string _themesRoot;
    private readonly string _cacheRoot;
    private readonly string[] _defaultThemeFiles = ["default-dark.zip", "default-light.zip"];
    private List<ThemeModel> _cachedThemes = [];

    public ThemeProvider(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        _themesRoot = Path.Combine(_pathsService.AssetsDirectory, "Themes");
        _cacheRoot = Path.Combine(_pathsService.CacheDirectory, "Themes");

        Directory.CreateDirectory(_themesRoot);
        Directory.CreateDirectory(_cacheRoot);
    }

    public List<ThemeModel> GetAllThemes()
    {
        var list = new List<ThemeModel>();
        if (!Directory.Exists(_themesRoot)) return list;

        try
        {
            if (Directory.Exists(_cacheRoot))
            {
                Directory.Delete(_cacheRoot, true);
                Directory.CreateDirectory(_cacheRoot);
            }
        }
        catch
        {
            Debug.WriteLine("Error occurred while cleaning up themes cache.");
        }

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
                Debug.WriteLine($"Error loading theme {zipPath}: {ex.Message}");
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

        _cachedThemes = res;
        return res;
    }

    public ThemeModel? GetThemeByFileName(string fileName)
    {
        return _cachedThemes.FirstOrDefault(t => t.ZipPath.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    public Task ImportThemeAsync(string themeFilePath)
    {
        if (string.IsNullOrEmpty(themeFilePath) || !File.Exists(themeFilePath))
            return Task.CompletedTask;

        var destPath = Path.Combine(_themesRoot, Path.GetFileName(themeFilePath));
        File.Copy(themeFilePath, destPath, overwrite: true);

        return Task.CompletedTask;
    }

    public ThemeModel? ExtractAndProcessTheme(string themeFileNameOrZipPath)
    {
        string zipPath = File.Exists(themeFileNameOrZipPath)
            ? themeFileNameOrZipPath
            : Path.Combine(_themesRoot, themeFileNameOrZipPath);

        if (!File.Exists(zipPath)) return null;

        var folderName = Path.GetFileNameWithoutExtension(zipPath);
        var themeCacheDir = Path.Combine(_cacheRoot, folderName);
        Directory.CreateDirectory(themeCacheDir);

        ZipFile.ExtractToDirectory(zipPath, themeCacheDir, overwriteFiles: true);

        var themeXamlPath = Path.Combine(themeCacheDir, "Theme.xaml");
        if (!File.Exists(themeXamlPath)) return null;
        
        var manifestPath = Path.Combine(themeCacheDir, "manifest.json");
        string name = folderName;
        string author = "Unknown";
        string description = string.Empty;
        string version = "1.0.0";

        if (File.Exists(manifestPath))
        {
            try
            {
                var json = File.ReadAllText(manifestPath, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var manifest = JsonSerializer.Deserialize<ThemeModel>(json, options);

                if (manifest != null)
                {
                    if (!string.IsNullOrWhiteSpace(manifest.Name)) name = manifest.Name;
                    if (!string.IsNullOrWhiteSpace(manifest.Author)) author = manifest.Author;
                    if (!string.IsNullOrWhiteSpace(manifest.Description)) description = manifest.Description;
                    if (!string.IsNullOrWhiteSpace(manifest.Version)) version = manifest.Version;
                }
            }
            catch
            {
                Debug.WriteLine("Error occurred while deserializing theme manifest.");
            }
        }
        
        string xamlContent = File.ReadAllText(themeXamlPath);
        var baseUri = new Uri(themeCacheDir + Path.DirectorySeparatorChar).AbsoluteUri;

        xamlContent = Regex.Replace(xamlContent, @"([\""']?)[\\/]?Fonts[\\/]", $"$1{baseUri}Fonts/", RegexOptions.IgnoreCase);
        xamlContent = Regex.Replace(xamlContent, @"([\""']?)[\\/]?Images[\\/]", $"$1{baseUri}Images/", RegexOptions.IgnoreCase);

        File.WriteAllText(themeXamlPath, xamlContent);
        
        string? bannerPath = null;
        var possibleDirs = new[] { themeCacheDir, Path.Combine(themeCacheDir, "Banner") };

        foreach (var dir in possibleDirs)
        {
            if (Directory.Exists(dir))
            {
                var file = Directory.GetFiles(dir)
                    .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

                if (file != null)
                {
                    bannerPath = file;
                    break;
                }
            }
        }

        return new ThemeModel
        {
            ZipPath = Path.GetFileName(zipPath),
            XamlPath = themeXamlPath,
            Name = name,
            Author = author,
            Description = description,
            Version = version,
            BannerPath = bannerPath
        };
    }
}
