using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity.Abstractions;

public interface IAuthService
{
    Task<UserAccount> LoginWithMicrosoftAsync();
    UserAccount LoginOffline(string nickname);
    Task<UserAccount> ValidateAndRefreshAccountAsync(UserAccount account);
}