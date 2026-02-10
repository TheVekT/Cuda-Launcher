using System.ComponentModel;

namespace Launcher.UI.WPF.Stores;

public class AppStore: INotifyPropertyChanged
{
    private object _currentOverlayView;
    private bool _isOverlayVisible;

    public AppStore()
    {
        //Initialize app state (not implemented yet)
    }
    
    
    //Getters and Setters
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