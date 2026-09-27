using System.Reflection;
using Launcher.Infrastructure.Updates.Abstractions;

namespace Launcher.Infrastructure.Updates;

public class AppVersionProvider : IAppVersionProvider
{
    private readonly Lazy<string> _currentVersionLazy = new(ResolveCurrentVersion);

    public string CurrentVersion => _currentVersionLazy.Value;

    private static string ResolveCurrentVersion()
    {
        var targetAssembly = Assembly.GetEntryAssembly() ?? typeof(AppVersionProvider).Assembly;

        var infoVersion = targetAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(infoVersion))
            return targetAssembly.GetName().Version?.ToString(3) ?? "0.0.0";

        return infoVersion.Split('+')[0].Trim();
    }
}
