namespace Launcher.Core.Services.System;

public static class LauncherPathsService
{
    public static string BaseDirectory { get; }
    public static string DataDirectory { get; }
    public static string UserDataDirectory { get; }
    public static string InstancesDirectory { get; }
    public static string AssetsDirectory { get; }

    static LauncherPathsService()
    {
        BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        DataDirectory = Path.Combine(BaseDirectory, "Data");

        UserDataDirectory = Path.Combine(DataDirectory, "UserData");
        InstancesDirectory = Path.Combine(DataDirectory, "Instances");
        AssetsDirectory = Path.Combine(BaseDirectory, "Assets");

        CreateDir(DataDirectory);
        CreateDir(UserDataDirectory);
        CreateDir(InstancesDirectory);
        CreateDir(AssetsDirectory);
    }

    private static void CreateDir(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}