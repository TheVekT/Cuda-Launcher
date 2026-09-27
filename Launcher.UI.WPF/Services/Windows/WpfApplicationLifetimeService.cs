using System.Windows;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Services.Windows;

public class WpfApplicationLifetimeService : IApplicationLifetimeService
{
    public void Shutdown(int exitCode = 0)
    {
        if (Application.Current == null)
        {
            Environment.Exit(exitCode);
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
            Application.Current.Shutdown(exitCode);
        else
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown(exitCode));
    }
}
