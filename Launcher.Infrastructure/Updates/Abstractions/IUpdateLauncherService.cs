using FluentResults;
using Launcher.Infrastructure.Updates.Models;

namespace Launcher.Infrastructure.Updates.Abstractions;

public interface IUpdateLauncherService
{
    Result LaunchUpdater(UpdateCheckResult updateResult);
    Result CleanupTempDirectory();
}