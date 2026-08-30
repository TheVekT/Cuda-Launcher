using Launcher.Updater.Models;

namespace Launcher.Updater.Services.Abstractions;

public interface IUpdateExecutionService
{
    Task<bool> ApplyUpdateAsync(
        UpdaterArgs args, 
        IProgress<UpdateProgressInfo>? progress = null, 
        CancellationToken cancellationToken = default);
}