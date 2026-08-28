using Launcher.UI.WPF.Services.Shell.Abstractions;

namespace Launcher.UI.WPF.Services.Shell;

public class NavigationService: INavigationService
{
    private Action<object>? _navigationAction;
    
    public void RegisterNavigationHandler(Action<object> action)
    {
        _navigationAction = action;
    }
    
    public void Navigate(object? viewModel)
    {
        if (viewModel != null)
        {
            _navigationAction?.Invoke(viewModel);
        }
    }
}