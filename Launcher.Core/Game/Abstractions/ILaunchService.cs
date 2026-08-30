using FluentResults;
using Launcher.Core.Common.Messages;
using Launcher.Core.Game.Models;
using Launcher.Core.Identity.Models;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Game.Abstractions;

public interface ILaunchService
{
    event Action<MinecraftInstance, GameCrashReport>? GameCrashed;
    
    Task<Result<GameLaunchResult>> LaunchGameAsync(
        MinecraftInstance instance, 
        UserAccount account, 
        GlobalLaunchSettings globalSettings, 
        IProgress<GameLaunchProgressMessage> progress);
}