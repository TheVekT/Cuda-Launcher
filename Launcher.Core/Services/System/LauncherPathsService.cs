using System;
using System.IO;

namespace Launcher.Core.Services.System;

public interface ILauncherPathsService
{
    string BaseDirectory { get; }
    string DataDirectory { get; }
    string UserDataDirectory { get; }
    string InstancesDirectory { get; }
    string AssetsDirectory { get; }
    string CacheDirectory { get; }
}

public class LauncherPathsService : ILauncherPathsService
{
    public string BaseDirectory { get; }
    public string DataDirectory { get; }
    public string UserDataDirectory { get; }
    public string InstancesDirectory { get; }
    public string AssetsDirectory { get; }
    public string CacheDirectory { get; }

    public LauncherPathsService()
    {
        BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        DataDirectory = Path.Combine(BaseDirectory, "Data");

        UserDataDirectory = Path.Combine(DataDirectory, "UserData");
        InstancesDirectory = Path.Combine(DataDirectory, "Instances");
        AssetsDirectory = Path.Combine(BaseDirectory, "Assets");
        
        CacheDirectory = Path.Combine(BaseDirectory, "Cache");

        CreateDir(DataDirectory);
        CreateDir(UserDataDirectory);
        CreateDir(InstancesDirectory);
        CreateDir(AssetsDirectory);
        CreateDir(CacheDirectory);
    }

    private void CreateDir(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}