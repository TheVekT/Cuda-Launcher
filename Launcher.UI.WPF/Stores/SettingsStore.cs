using System.Collections.ObjectModel;
using System.ComponentModel;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Stores;

public class SettingsStore: INotifyPropertyChanged
{
    //Services
    private readonly ThemeService _themeService;
    
    //Attributes
    private double _uiScale = 1.0;
    private string _currentThemePath;
    
    //Collections
    public ObservableCollection<ThemeModel> AvailableThemes { get; set; } = new ObservableCollection<ThemeModel>();
    
    public SettingsStore(ThemeService themeService)
    {
        _themeService = themeService;

        _currentThemePath = "";
        //Load settings from storage (not implemented yet)
    }
    
    
    //Getters and Setters
    
    public string CurrentThemePath
    {
        get => _currentThemePath;
        set
        {
            if (_currentThemePath != value)
            {
                _currentThemePath = value;
                OnPropertyChanged(nameof(CurrentThemePath));
                    
                // Сразу применяем тему
                _themeService.ChangeTheme(_currentThemePath);
            }
        }
    }
    public double UiScale
    {
        get => _uiScale;
        set { if (Math.Abs(_uiScale - value) > 0.001) { _uiScale = value; OnPropertyChanged(nameof(UiScale)); } }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}