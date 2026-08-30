namespace Launcher.UI.WPF.Services.Windows.Abstractions;

public interface IDispatcherService
{
    void Invoke(Action action);
    
    Task InvokeAsync(Action action);
    
    Task InvokeAsync(Func<Task> asyncAction);
}