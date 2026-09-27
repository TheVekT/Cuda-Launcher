namespace Launcher.UI.WPF.Services.Windows.Abstractions;

public interface IApplicationLifetimeService
{
    void Shutdown(int exitCode = 0);
}
