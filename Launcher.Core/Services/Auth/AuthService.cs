using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.MsalClient;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace Launcher.Core.Services.Auth
{
    public class AuthService : IAuthService
    {
        // Це стандартний ClientID для Minecraft лаунчерів, але краще зареєструвати свій в Azure
        // Для тестування можна використовувати дефолтний від CmlLib або цей
        private const string ClientId = "00000000402b5328"; 

        public async Task<MSession> LoginWithMicrosoftAsync()
        {
            // Налаштування логіна
            var loginHandler = JELoginHandlerBuilder.BuildDefault();

            // Цей метод автоматично:
            // 1. Відкриє браузер для входу в Microsoft
            // 2. Пройде всі етапи Xbox аутентифікації
            // 3. Поверне готову сесію
            try 
            {
                var session = await loginHandler.AuthenticateInteractively();
                return session;
            }
            catch (Exception ex)
            {
                // Логування помилки тут
                throw new Exception($"Microsoft Login failed: {ex.Message}");
            }
        }
    }
}