using Launcher.Core.Common.Models;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Mods.Abstractions;

public interface IModrinthService
{
    Task<bool> InstallEssentialApisAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress);
    Task<bool> InstallPerformanceModsAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress);
}