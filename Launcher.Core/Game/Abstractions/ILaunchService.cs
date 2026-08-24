using System.Diagnostics;
using FluentResults;
using Launcher.Core.Common.Messages;
using Launcher.Core.Identity.Models;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Game.Abstractions;

public interface ILaunchService
{
    Task<Result<Process>> LaunchGameAsync(MinecraftInstance instance, UserAccount account, GlobalLaunchSettings globalSettings, IProgress<GameLaunchProgressMessage> progress);
}