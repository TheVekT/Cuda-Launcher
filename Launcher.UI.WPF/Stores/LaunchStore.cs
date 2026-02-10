using System.ComponentModel;
using Launcher.Core.Services.Game;

namespace Launcher.UI.WPF.Stores;

public class LaunchStore: INotifyPropertyChanged
{
    //Services
    private readonly ILaunchService _launchService;

    public LaunchStore(ILaunchService launchService)
    {
        _launchService = launchService;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}