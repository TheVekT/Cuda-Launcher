using System.ComponentModel;
using System.Windows.Input;

namespace Launcher.UI.WPF.ViewModels.Settings;

public class CustomizationSettingsVM : INotifyPropertyChanged
{
    
    public ICommand SelfCloseCommand { get; set; }

    public CustomizationSettingsVM (){
        
    }
    
    

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}