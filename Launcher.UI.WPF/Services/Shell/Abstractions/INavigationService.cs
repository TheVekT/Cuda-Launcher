namespace Launcher.UI.WPF.Services.Shell.Abstractions;

public interface INavigationService
{
    void RegisterNavigationHandler(Action<object> action);
    void Navigate(object? viewModel);
}