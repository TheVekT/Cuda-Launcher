namespace Launcher.Infrastructure.Updates.Abstractions;

public interface IAppVersionProvider
{
    string CurrentVersion { get; }
}
