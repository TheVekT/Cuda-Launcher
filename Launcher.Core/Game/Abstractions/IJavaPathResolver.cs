using CmlLib.Core;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Game.Abstractions;

public interface IJavaPathResolver
{
    string? ResolveJavaPath(MinecraftPath globalMcPath, MinecraftInstance instance);
    string? ResolveJavaPath(MinecraftPath globalMcPath, string gameVersion);
}
