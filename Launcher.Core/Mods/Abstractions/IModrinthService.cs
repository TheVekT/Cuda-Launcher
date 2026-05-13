using Launcher.Core.Common.Models;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Mods.Abstractions;

public interface IModrinthService
{
    Task InstallEssentialApisAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress = null);
    Task InstallPerformanceModsAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress = null);
}