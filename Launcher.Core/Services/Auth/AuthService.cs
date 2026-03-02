using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Launcher.Core.Models;

namespace Launcher.Core.Services.Auth
{
    public interface IAuthService
    {
        Task<UserAccount> LoginWithMicrosoftAsync();
        UserAccount LoginOffline(string nickname);
        Task<UserAccount> ValidateAndRefreshAccountAsync(UserAccount account);
    }

    public class AuthService : IAuthService
    {
        private readonly JELoginHandler _loginHandler;

        public AuthService()
        {
            _loginHandler = JELoginHandlerBuilder.BuildDefault();
        }
        
        public async Task<UserAccount> LoginWithMicrosoftAsync()
        {
            try 
            {
                var session = await _loginHandler.AuthenticateInteractively();
                
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
            var offlineSession = MSession.CreateOfflineSession(nickname);
            return new UserAccount(offlineSession.Username, offlineSession.UUID, offlineSession.AccessToken, isOffline: true);
        }

        public async Task<UserAccount> ValidateAndRefreshAccountAsync(UserAccount account)
        {
            if (account == null) return null;
            if (account.IsOffline) return account;

            if (string.IsNullOrEmpty(account.AccessToken))
            {
                throw new UnauthorizedAccessException("Token is missing or decryption failed.");
            }
            
            try
            {
                Debug.WriteLine($"[Auth] Проверяем токен {account.Username} напрямую через Mojang API...");
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
                
                var response = await httpClient.GetAsync("https://api.minecraftservices.com/minecraft/profile");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("name", out var nameElement))
                    {
                        account.Username = nameElement.GetString();
                    }
                    
                    Debug.WriteLine($"[Auth] Токен валиден. Актуальный ник: {account.Username}");
                    return account;
                }
                else
                {
                    // Сервер вернул ошибку (токен протух, обычно они живут 24 часа).
                    throw new Exception("Minecraft Access Token has expired. Re-login required.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auth Error] {ex}");
                throw new Exception($"Session validation failed: {ex.Message}");
            }
        }
    }
}