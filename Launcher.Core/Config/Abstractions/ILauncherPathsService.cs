namespace Launcher.Core.Config.Abstractions;

public interface ILauncherPathsService
{
    string BaseDirectory { get; }
    string DataDirectory { get; }
    string UserDataDirectory { get; }
    string InstancesDirectory { get; }
    string AssetsDirectory { get; }
    string CacheDirectory { get; }
}
