using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Infrastructure.Customization.Abstractions;
using Launcher.Infrastructure.Customization.Models;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Abstractions;

namespace Launcher.UI.WPF.Services;

public class ThemeService: IThemeService
{
    private readonly IThemeProvider _themeProvider;
    private List<ThemeModel> _availableThemes = new();
    private ThemeModel? _currentTheme;
    private ResourceDictionary? _currentThemeDictionary;

    public ThemeModel? CurrentTheme => _currentTheme;

    public ThemeService(IThemeProvider themeProvider)
    {
        _themeProvider = themeProvider;
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

    private void ReplaceApplicationResources(ResourceDictionary newDict)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;

        var oldBrushes = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Brushes.xaml"));
        if (oldBrushes != null)
        {
            dicts.Remove(oldBrushes);
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
