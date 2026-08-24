using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Infrastructure.Themes.Abstractions;
using Launcher.Infrastructure.Themes.Models;
using Launcher.UI.WPF.Messages;

namespace Launcher.UI.WPF.Services;

public class ThemeService
{
    private const string DefaultThemeFileName = "default-dark.zip";

    private readonly IThemeProvider _themeProvider;
    private List<ThemeModel> _availableThemes = new();
    private ThemeModel? _currentTheme;
    private ResourceDictionary? _currentThemeDictionary;
    private ResourceDictionary? _fallbackThemeDictionary;

    public ThemeModel? CurrentTheme => _currentTheme;

    public ThemeService(IThemeProvider themeProvider)
    {
        _themeProvider = themeProvider;
        EnsureFallbackThemeLoaded();
    }

    public ThemeModel? GetThemeByFileName(string fileName)
    {
        return _themeProvider.GetThemeByFileName(fileName) 
               ?? _availableThemes.FirstOrDefault(t => t.ZipPath.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task ImportTheme(string themeFilePath)
    {
        if (string.IsNullOrEmpty(themeFilePath) || !File.Exists(themeFilePath)) return;

        await _themeProvider.ImportThemeAsync(themeFilePath);
        WeakReferenceMessenger.Default.Send(new ThemeImportedMessage());
    }

    public List<ThemeModel> ReloadThemes()
    {
        _availableThemes = _themeProvider.GetAllThemes();
        return _availableThemes;
    }

    public void ChangeTheme(string themeFileName)
    {
        if (string.IsNullOrEmpty(themeFileName)) return;

        var theme = GetThemeByFileName(themeFileName);
        if (theme == null || !File.Exists(theme.XamlPath))
        {
            theme = _themeProvider.ExtractAndProcessTheme(themeFileName);
        }

        if (theme == null || !File.Exists(theme.XamlPath)) return;

        try
        {
            string xamlContent = File.ReadAllText(theme.XamlPath);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xamlContent)))
            {
                var parserContext = new ParserContext();
                parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");

                var newDict = (ResourceDictionary)XamlReader.Load(stream, parserContext);
                ReplaceApplicationResources(newDict);
            }

            _currentTheme = theme;
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(themeFileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to apply theme: {ex.Message}");
        }
    }

    private void EnsureFallbackThemeLoaded()
    {
        if (_fallbackThemeDictionary != null) return;

        try
        {
            var defaultTheme = _themeProvider.GetThemeByFileName(DefaultThemeFileName) 
                               ?? _themeProvider.ExtractAndProcessTheme(DefaultThemeFileName);

            if (defaultTheme != null && File.Exists(defaultTheme.XamlPath))
            {
                string xamlContent = File.ReadAllText(defaultTheme.XamlPath);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xamlContent));
                var parserContext = new ParserContext();
                parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");

                _fallbackThemeDictionary = (ResourceDictionary)XamlReader.Load(stream, parserContext);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load fallback theme: {ex.Message}");
        }
    }

    private void ReplaceApplicationResources(ResourceDictionary newDict)
    {
        EnsureFallbackThemeLoaded();

        var dicts = Application.Current.Resources.MergedDictionaries;

                var oldBrushes = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Brushes.xaml"));
        if (oldBrushes != null)
        {
            dicts.Remove(oldBrushes);
        }

                if (_fallbackThemeDictionary != null && !dicts.Contains(_fallbackThemeDictionary))
        {
            dicts.Add(_fallbackThemeDictionary);
        }

                if (_currentThemeDictionary != null && dicts.Contains(_currentThemeDictionary))
        {
            var index = dicts.IndexOf(_currentThemeDictionary);
            dicts[index] = newDict;
        }
        else
        {
            dicts.Add(newDict);
        }

        _currentThemeDictionary = newDict;

                dicts.Add(new ResourceDictionary { Source = new Uri("Resources/Styles/Brushes.xaml", UriKind.Relative) });
    }
}
