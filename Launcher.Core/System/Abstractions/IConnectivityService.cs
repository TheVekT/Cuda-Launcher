using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Core.System.Abstractions;

public interface IConnectivityService
{
    Task<bool> CheckInternetAccessAsync(CancellationToken cancellationToken = default);
}
