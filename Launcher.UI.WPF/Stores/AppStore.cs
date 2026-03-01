using System.ComponentModel;
using System.Windows;

namespace Launcher.UI.WPF.Stores;

public class AppStore: INotifyPropertyChanged
{
    private object _currentOverlayView;
    private bool _isOverlayVisible;
    private string _currentBannerPath = "Assets/Images/banner-default.jpg";
    
    private bool _isDownloading;
    private bool _isGameRunning;
    
    public Visibility DownloadPanelVisibility => _isDownloading ? Visibility.Visible : Visibility.Collapsed;

    public AppStore()
    {
        //Initialize app state (not implemented yet)
    }
    
    
    //Getters and Setters
    public bool IsCurrentInstanceInProcess
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

    public string CurrentBannerPath
    {
        get => _currentBannerPath;
        set
        {
            if (_currentBannerPath == value) return;
            if (value is null)
            {
                _currentBannerPath = "Assets/Images/banner-default.jpg";
            }
            else
            {
                _currentBannerPath = value; 
            }
            OnPropertyChanged(nameof(CurrentBannerPath));
        }
    }
    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set { _isOverlayVisible = value; OnPropertyChanged(nameof(IsOverlayVisible)); }
    }
    public object CurrentOverlayView
    {
        get => _currentOverlayView;
        set { _currentOverlayView = value; OnPropertyChanged(nameof(CurrentOverlayView)); IsOverlayVisible = _currentOverlayView != null; }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}