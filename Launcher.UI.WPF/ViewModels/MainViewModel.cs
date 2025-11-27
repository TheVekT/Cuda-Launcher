
using System.ComponentModel;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ThemeService _themeService;

    private double _uiScale = 1.0;
    private bool _isOverlayVisible;
    private object _currentOverlayView;
    public ICommand ChangeThemeCommand { get; }
    private string _currentThemePath = "/Assets/Themes/default-dark.xaml";
    public double UiScale
    {
        get => _uiScale;
        set
        {
            if (_uiScale != value)
            {
                _uiScale = value;
                OnPropertyChanged(nameof(UiScale));
            }
        }
    }
    public string CurrentThemePath
        {
            get => _currentThemePath;
            set
            {
                if (_currentThemePath != value)
                {
                    _currentThemePath = value;
                    OnPropertyChanged(nameof(CurrentThemePath));
                    
                    _themeService.ChangeTheme(_currentThemePath);
                }
            }
        }
    
    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set
        {
            _isOverlayVisible = value;
            OnPropertyChanged(nameof(IsOverlayVisible));
        }
    }
    
    public object CurrentOverlayView
    {
        get => _currentOverlayView;
        set
        {
            _currentOverlayView = value;
            OnPropertyChanged(nameof(CurrentOverlayView));
            
            IsOverlayVisible = _currentOverlayView != null;
        }
    }
    
    public ICommand CloseOverlayCommand { get; }
    
    public ICommand OpenSettingsCommand { get; }
    
    public ICommand OpenLoginCommand { get; }
    
    public MainViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        OpenSettingsCommand = new RelayCommand(o => 
        {
            CurrentOverlayView = new Resources.Overlay.SettingsMenu();
        });
        
        OpenLoginCommand = new RelayCommand(o => 
        {
            CurrentOverlayView = new Resources.Overlay.LoginMenu();
        });
        
        _themeService.ChangeTheme(_currentThemePath);
        ChangeThemeCommand = new RelayCommand(path => 
        {
            if (path is string themePath)
            {
                _themeService.ChangeTheme(themePath);
            }
        });

        CloseOverlayCommand = new RelayCommand(o => 
        {
            CurrentOverlayView = null;
        });
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}