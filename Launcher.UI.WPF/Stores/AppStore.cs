using System.ComponentModel;

namespace Launcher.UI.WPF.Stores;

public class AppStore: INotifyPropertyChanged
{
    private object _currentOverlayView;
    private bool _isOverlayVisible;
    private string _currentBannerPath = "Assets/Images/banner-default.jpg";

    public AppStore()
    {
        //Initialize app state (not implemented yet)
    }
    
    
    //Getters and Setters
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