
using System.ComponentModel;

namespace Launcher.UI.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{


    private double _uiScale = 1.0;

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
    
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}