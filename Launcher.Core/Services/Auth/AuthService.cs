using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.MsalClient;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;
using Launcher.Core.Models; // Необходимо для UserAccount

namespace Launcher.Core.Services.Auth
{
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
            // Логика генерации UUID перенесена сюда
            string fakeUuid = Guid.NewGuid().ToString(); 
    
            return new UserAccount(
                nickname, 
                fakeUuid, 
                token: string.Empty, 
                isOffline: true);
        }
    }
}