
using System.ComponentModel;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;

namespace Launcher.UI.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{


    private double _uiScale = 1.0;
    private bool _isOverlayVisible;
    private object _currentOverlayView;
    
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
    
    public MainViewModel()
    {
        OpenSettingsCommand = new RelayCommand(o => 
        {
            CurrentOverlayView = new Resources.Overlay.SettingsMenu();
        });

        /*
        OpenLoginCommand = new RelayCommand(o => 
        {
            CurrentOverlayView = new Resources.Overlay.LoginMenu(); 
        });*/

        // Закрыть всё
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