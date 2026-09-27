namespace Launcher.Updater.Services.Abstractions;

public interface IAppVersionProvider
{
    string CurrentVersion { get; }
}
