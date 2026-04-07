using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.UI.WPF.ViewModels.Settings;

public partial class CustomizationSettingsVM : ObservableObject
{
    
    public ICommand SelfCloseCommand { get; set; }

    public CustomizationSettingsVM (){
        
    }
}