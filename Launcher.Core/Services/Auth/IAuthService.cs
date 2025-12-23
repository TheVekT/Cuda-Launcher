using CmlLib.Core.Auth;
using System.Threading.Tasks;

namespace Launcher.Core.Services.Auth
{
    public interface IAuthService
    {
        Task<MSession> LoginWithMicrosoftAsync();
    }
}