using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.MsalClient;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;
using Launcher.Core.Models; // Необходимо для UserAccount

namespace Launcher.Core.Services.Auth
{
    public interface IAuthService
    {
        Task<UserAccount> LoginWithMicrosoftAsync();
        UserAccount LoginOffline(string nickname);
    }
    public class AuthService : IAuthService
    {
        private const string ClientId = "00000000402b5328"; 

        public async Task<UserAccount> LoginWithMicrosoftAsync()
        {
            var loginHandler = JELoginHandlerBuilder.BuildDefault();
            
            try 
            {
                var session = await loginHandler.AuthenticateInteractively();
                
                // Преобразование MSession в UserAccount внутри сервиса
                return new UserAccount(
                    session.Username, 
                    session.UUID, 
                    session.AccessToken, 
                    isOffline: false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Microsoft Login failed: {ex.Message}");
            }
        }

        public UserAccount LoginOffline(string nickname)
        {
            // Используем встроенный метод CmlLib для генерации правильной оффлайн-сессии.
            // Он сам создаст UUID (на основе ника) и валидный фиктивный токен.
            var offlineSession = MSession.CreateOfflineSession(nickname);
    
            return new UserAccount(
                offlineSession.Username, 
                offlineSession.UUID, 
                offlineSession.AccessToken, // <-- Тут теперь будет "access_token" или "0", а не null/empty
                isOffline: true);
        }
    }
}