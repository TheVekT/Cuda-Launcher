using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.ViewModels.Settings;

public class SettingsVM: INotifyPropertyChanged
{
    private readonly SettingsStore _settingsStore;
    private readonly ThemeService _themeService;
    
    private readonly AppStore _appStore;
    
    public SettingsStore SettingsStore => _settingsStore;
    public AppStore AppStore => _appStore;
    
    //Collections
    
    
    //Attributes
    

    
    
    //Events
    public event Action RequestClose;
    
    //Commands
    
    public ICommand ImportThemeCommand { get; }
    public ICommand ImportLanguageCommand { get; }
    public ICommand CloseSelfCommand { get; }
    public ICommand RefreshThemesCommand { get; }
    public ICommand SelectThemeCommand { get; }
    
    public SettingsVM(SettingsStore settingsStore, ThemeService themeService, AppStore appStore)
    {
        _settingsStore = settingsStore;
        _themeService = themeService;
        _appStore = appStore;

        
            
        CloseSelfCommand = new RelayCommand(o => RequestClose?.Invoke());
        RefreshThemesCommand = new RelayCommand(o => LoadThemes());
        SelectThemeCommand = new RelayCommand(param => 
        {
            if (param is ThemeModel theme)
            {
                _settingsStore.CurrentThemePath = theme.ZipPath;
                _appStore.ThemeBannerPath = theme.BannerPath;
            }
        });
        ImportThemeCommand = new RelayCommand(async o => await ExecuteImportTheme(o));
        ImportLanguageCommand = new RelayCommand(async o => await ExecuteImportLanguage(o));
            
        LoadThemes();
    }

    private async Task ExecuteImportTheme(object o)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Zip files (*.zip)|*.zip",
            Title = "Select a theme zip file"
        };
        if (openFileDialog.ShowDialog() == true)        {
            var selectedFile = openFileDialog.FileName;
            await _themeService.ImportTheme(selectedFile);
            var lastThemePath = _settingsStore.CurrentThemePath;
            LoadThemes();
            _settingsStore.CurrentThemePath = lastThemePath;
        }   
    }

    private async Task ExecuteImportLanguage(object o)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Select a localization json file"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var selectedFile = openFileDialog.FileName;
            
            await LocalizationService.Instance.ImportLocalization(selectedFile);
            var lastLangCode = _settingsStore.SelectedLanguage.Code;
            _settingsStore.AvailableLanguages.Clear(); 
            var langs = LocalizationService.Instance.GetAvailableLanguages();
            foreach (var lang in langs) _settingsStore.AvailableLanguages.Add(lang);
            _settingsStore.SelectedLanguage = _settingsStore.AvailableLanguages.FirstOrDefault(l => l.Code == lastLangCode);
        }
    }
    
    public void LoadThemes()
    {
        var themes = _themeService.ReloadThemes(); // Это распакует все темы
        _settingsStore.AvailableThemes.Clear();
        foreach (var theme in themes)
        {
            _settingsStore.AvailableThemes.Add(theme);
        }
    }
    
    //Getters and Setters
    

    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}