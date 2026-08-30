
namespace Launcher.Core.System.Abstractions;

public interface IConnectivityService
{
    Task<bool> CheckInternetAccessAsync(CancellationToken cancellationToken = default);
}
