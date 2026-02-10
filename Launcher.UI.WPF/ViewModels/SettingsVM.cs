using System.ComponentModel;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class SettingsVM: INotifyPropertyChanged
{
    private readonly SettingsStore _settingsStore;
    
    public SettingsStore SettingsStore => _settingsStore;
    
    //Events
    public event Action RequestClose;
    
    //Commands
    public ICommand CloseSelfCommand { get; }
    
    public SettingsVM(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        
        CloseSelfCommand = new RelayCommand(o => RequestClose?.Invoke());
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}