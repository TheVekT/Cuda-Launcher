using CmlLib.Core.Auth;
using System.Threading.Tasks;
using Launcher.Core.Models;

namespace Launcher.Core.Services.Auth
{
    public interface IAuthService
    {
        Task<UserAccount> LoginWithMicrosoftAsync();
        UserAccount LoginOffline(string nickname);
    }
}