using System.Windows;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Services.Windows;

public class WpfDispatcherService : IDispatcherService
{
    public void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    public Task InvokeAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            return dispatcher.InvokeAsync(action).Task;
        }
        
        action();
        return Task.CompletedTask;
    }

    public Task InvokeAsync(Func<Task> asyncAction)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            return dispatcher.InvokeAsync(asyncAction).Task.Unwrap();
        }

        return asyncAction();
    }
}