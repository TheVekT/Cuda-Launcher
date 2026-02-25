using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class SettingsVM: INotifyPropertyChanged
{
    private readonly SettingsStore _settingsStore;
    private readonly ThemeService _themeService;
    
    private readonly AppStore _appStore;
    
    public SettingsStore SettingsStore => _settingsStore;
    public AppStore AppStore => _appStore;
    
    //Collections
    
    //Attributes
    private LanguageModel _selectedLanguage;
    
    //Events
    public event Action RequestClose;
    
    //Commands
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
                _appStore.CurrentBannerPath = theme.BannerPath;
            }
        });
        SelectedLanguage = _settingsStore.AvailableLanguages.FirstOrDefault(l => l.Code == "en-US") 
                           ?? _settingsStore.AvailableLanguages.FirstOrDefault();
        
        LoadThemes();
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
    
    public LanguageModel SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value && value != null)
            {
                _selectedLanguage = value;
                OnPropertyChanged(nameof(SelectedLanguage));
                Console.WriteLine($"Selected language: {value.Name}, code: {value.Code}");
                LocalizationService.Instance.LoadLanguage(value.Code);
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}