using System.Diagnostics;
using Launcher.Core.Identity.Models;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Game.Abstractions;

public interface ILaunchService
{
    Task<Process> LaunchGameAsync(MinecraftInstance instance, UserAccount account, GlobalLaunchSettings globalSettings);
}