using System.ComponentModel;
using System.Windows;

namespace Launcher.UI.WPF.Stores;

public class AppStore: INotifyPropertyChanged
{
    private object? _currentOverlayView;
    private bool _isOverlayVisible;
    private string _themeBannerPath = "Assets/Images/banner-default.jpg";
    
    private bool _isDownloading;
    private bool _isGameRunning;
    private bool _isDragDropActive;
    
    public Visibility DownloadPanelVisibility => _isDownloading ? Visibility.Visible : Visibility.Collapsed;

    public AppStore()
    {
        //Initialize app state (not implemented yet)
    }
    
    
    //Getters and Setters
    public bool IsCurrentInstanceProcessing
    {
        get
        {
            if (IsGameRunning || IsDownloading) return true;
            return false;
        }
    }
    
    public bool IsGameRunning
    {
        get => _isGameRunning;
        set
        {
            if (_isGameRunning != value)
            {
                _isGameRunning = value;
                OnPropertyChanged(nameof(IsGameRunning));
            }
        }
    }
    
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (_isDownloading != value)
            {
                _isDownloading = value;
                OnPropertyChanged(nameof(IsDownloading));
                OnPropertyChanged(nameof(DownloadPanelVisibility)); 
            }
        }
    }

    public bool IsDragDropActive
    {
        get => _isDragDropActive;
        set
        {
            if (_isDragDropActive != value)
            {
                _isDragDropActive = value;
                OnPropertyChanged(nameof(IsDragDropActive));
            }
        }
    }

    public string ThemeBannerPath
    {
        get => _themeBannerPath;
        set
        {
            if (_themeBannerPath == value) return;
            if (value is null)
            {
                _themeBannerPath = "Assets/Images/banner-default.jpg";
            }
            else
            {
                _themeBannerPath = value; 
            }
            OnPropertyChanged(nameof(CurrentBannerPath));
            OnPropertyChanged(nameof(ThemeBannerPath));
        }
    }

    public string CurrentBannerPath
    {
        get
        {
            return ThemeBannerPath;
        }
    }

    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set { _isOverlayVisible = value; OnPropertyChanged(nameof(IsOverlayVisible)); }
    }
    public object? CurrentOverlayView
    {
        get => _currentOverlayView;
        set { _currentOverlayView = value; OnPropertyChanged(nameof(CurrentOverlayView)); IsOverlayVisible = _currentOverlayView != null; }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}