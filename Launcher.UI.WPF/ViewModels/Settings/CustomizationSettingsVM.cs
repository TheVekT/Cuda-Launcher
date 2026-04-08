using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Messages;

namespace Launcher.UI.WPF.ViewModels.Settings;

public partial class CustomizationSettingsVM : ObservableObject
{

    public CustomizationSettingsVM (){
        
    }

    [RelayCommand]
    private void SelfClose() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
}