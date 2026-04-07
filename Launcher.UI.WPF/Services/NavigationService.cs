
namespace Launcher.UI.WPF.Services;

public class NavigationService
{
    private Action<object>? _navigationAction;
    
    public void RegisterNavigationHandler(Action<object> action)
    {
        _navigationAction = action;
    }
    
    public void Navigate(object viewModel)
    {
        if (viewModel != null)
        {
            _navigationAction?.Invoke(viewModel);
        }
    }
}