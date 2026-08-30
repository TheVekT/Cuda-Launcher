using FluentResults;
using Launcher.Infrastructure.Updates.Models;

namespace Launcher.Infrastructure.Updates.Abstractions;

public interface IUpdateCheckerService
{
    Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(
        string currentVersion, 
        bool includePrereleases = false, 
        CancellationToken cancellationToken = default);
}