using System.ComponentModel;
using Launcher.Core.Services.Game;

namespace Launcher.UI.WPF.Stores;

public class LaunchStore
{
    //Services
    private readonly ILaunchService _launchService;

    public LaunchStore(ILaunchService launchService)
    {
        _launchService = launchService;
    }
}