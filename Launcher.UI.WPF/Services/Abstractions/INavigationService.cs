namespace Launcher.UI.WPF.Services.Abstractions;

public interface INavigationService
{
    void RegisterNavigationHandler(Action<object> action);
    void Navigate(object viewModel);
}