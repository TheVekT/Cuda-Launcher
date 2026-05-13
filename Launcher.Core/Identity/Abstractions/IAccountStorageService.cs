using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity.Abstractions;

public interface IAccountStorageService
{
    void SaveAccounts(IEnumerable<UserAccount> accounts);
    List<UserAccount> LoadAccounts();
}